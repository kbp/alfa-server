﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Linq;
using AlfaServer.Entities;
using NLog;

namespace AlfaServer.Models
{
    public class MoxaSerialPort : IPort
    {
        private readonly Object _controllerLock = new Object();

        public MoxaSerialPort(string portName)
        {
            MxComPortConfig mxComPortConfig = new MxComPortConfig(portName);
            mxComPortConfig.SetRs422();

            _serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
            {
                RtsEnable = true,
                ReadTimeout = 1000
            };

            _config = Configuration.GetInstance();
            _portName = portName;

            try
            {
                _serialPort.Open();
                _logger.Info("_serialPort.Open() is open = " + _serialPort.IsOpen); 

                AlfaEntities alfaEntities = new AlfaEntities();

                var floors = from floorse in alfaEntities.Floors
                             where floorse.ComPort == portName
                             select floorse;

                foreach (Floors floorse in floors)
                {
                    if (floorse.Online == false)
                    {
                        floorse.Online = true;
                        alfaEntities.SaveChanges();
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error((exception.ToString()));

                AlfaEntities alfaEntities = new AlfaEntities();

                var floors = from floorse in alfaEntities.Floors
                             where floorse.ComPort == portName
                             select floorse;

                foreach (Floors floorse in floors)
                {
                    if (floorse.Online == true)
                    {
                        //floorse.Online = false;
                    }
                }

                alfaEntities.SaveChanges();
            }

            _serialPort.ReadTimeout = _readTimeout;
            _serialPort.WriteTimeout = _writeTimeout;
        }

        public void SetReadTimeout(int timeout)
        {
            _readTimeout = timeout;
        }
        public void SetWriteTimeout(int timeout)
        {
            _writeTimeout = timeout;
        }

        private int _readTimeout = Configuration.GetInstance().ReadTimeout;
        private int _writeTimeout = Configuration.GetInstance().WriteTimeout;
        
        private string _portName;

        private Configuration _config;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly SerialPort _serialPort;

        private string _currentMethodName;
        /// <summary>
        /// считывает значения состояния датчиков и номер последнего ключа с контроллера
        /// возвращает byte[0] массив если в процессе считывания возникла ошибка
        /// </summary>
        /// <returns> byte[2] </returns>
        public byte[] GetStateOfSensorAndNumberLastKey(byte controllerNumber)
        {
            _currentMethodName = MethodBase.GetCurrentMethod().Name;

            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 243;
            package[2] = GetCheckSum(package);

            lock (_controllerLock)
            {
                try
                {
                    System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    WriteBufferToPort(package);
//                    _logger.Info("write byte " + stopwatch.Elapsed);
                }
                catch (Exception)
                {
                    return new byte[0];
                }

                System.Diagnostics.Stopwatch sw = new Stopwatch();
                sw.Start();
                ReadData readData = ReadBufferFromPort(3);
//                _logger.Info("read byte " + sw.Elapsed);
                

                if (readData.IsValid)
                {
                    return readData.Buffer;
                }

            }

            return new byte[0];
        }

        /// <summary>
        /// чтение ключей с контроллера
        /// </summary>
        /// <returns></returns>
        public Dictionary<byte, ReadingKey> GetAllKeys(byte controllerNumber)
        {
            _currentMethodName = MethodBase.GetCurrentMethod().Name;

            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 5;
            package[2] = GetCheckSum(package);

            lock (_controllerLock)
            {
                Dictionary<byte, ReadingKey> keys = new Dictionary<byte, ReadingKey>();

                try
                {
                    WriteBufferToPort(package);
                }
                catch (Exception)
                {

                    for (byte keyNumber = 0; keyNumber < _config.CountKeysCells; keyNumber++)
                    {
                        keys.Add(keyNumber, new ReadingKey(false, new byte[0]));
                    }
                    return keys;
                }

                try
                {
                    byte[] keysBuffer = new byte[72];
                    Thread.Sleep(_config.IntervalReadingAllKeys);
                    _serialPort.Read(keysBuffer, 0, keysBuffer.Length);

                    byte position = 0;
                    for (byte keyNumber = 0; keyNumber < _config.CountKeysCells; keyNumber++)
                    {
                        byte sum = 0;
                        byte[] key = new byte[5];
                        for (int i = position; i < position + 5; i++)
                        {
                            key[i - position] = keysBuffer[i];
                            sum += keysBuffer[i];
                        }

                        if (sum == keysBuffer[position + 5])
                        {
                            keys.Add(keyNumber, new ReadingKey(true, key));
                        }
                        else
                        {
                            keys.Add(keyNumber, new ReadingKey(false, new byte[0]));
                        }

                        position += 6;
                    }

                    return keys;
                }
                catch (TimeoutException e)
                {
                    _logger.Debug("error read all keys from {0}. controller number {1}\n" + e, _portName, _currentDoorNumber);
                    return keys;
                }
            }
        }

        /// <summary>
        /// 1.7.    Получить код последнего считанного ключа.
        /// </summary>
        /// <param name="controllerNumber"> номер контролера </param>
        /// <returns> массив 5 байт </returns>
        public byte[] GetLastKey(byte controllerNumber)
        {
            _currentMethodName = MethodBase.GetCurrentMethod().Name;

            byte[] package = new byte[3];

            package[0] = controllerNumber;
            package[1] = 123;
            package[2] = GetCheckSum(package);

            // блокировка чтения/записи ком порта одновременно из нескольких потоков
            lock (_controllerLock)
            {
                WriteBufferToPort(package);

                ReadData readData = ReadBufferFromPort(6);

                if (readData.IsValid)
                {
                    return readData.Buffer;
                }

            }

            return new byte[0];
        }


        /// <summary>
        /// функция подсчета контрольной суммы
        /// </summary>
        /// <param name="package"> массив для которого будет расчитана контрольная сумма </param>
        /// <returns> контрольная сумма </returns>
        private static byte GetCheckSum(byte[] package)
        {
            byte checkSum = 0;
            for (byte i = 0; i < package.Length; i++)
            {
                checkSum += package[i];
            }

            return checkSum;
        }

        private byte _currentDoorNumber;

        /// <summary>
        /// Передача данных на ком порт
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteBufferToPort(byte[] buffer)
        {
//            Thread.Sleep(100);

            // todo может быть придется проверять на какой контроллер посылался последний запрос. и ставить таймаут
            // при каждой записи в синхронно ожидается ответ порта
            // номер текущего контроллера двери используется для удаление не отвечающих дверей
            _currentDoorNumber = buffer[0];

            try
            {
                _serialPort.Write(buffer, 0, buffer.Length);
            }
            catch (Exception exception)
            {
                _logger.Error("error write operation from method = {0} \n port = {1} \n controller = {2} \n" + exception, _currentMethodName, _portName, _currentDoorNumber);
                throw;

            }

        }

        /// <summary>
        /// в случае совпадения контрольной суммы последний байт равен 1
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private ReadData ReadBufferFromPort(byte size)
        {
            //todo oн может падать 
            Thread.Sleep(_config.IntervalSleepTime / 3);

            ReadData readData = new ReadData();

            byte[] readBuffer = new byte[size];

            try
            {
                _serialPort.Read(readBuffer, 0, size);
                _lastRespondedController = _currentDoorNumber;
            }
            catch (Exception exception)
            {
                _logger.Error("error read operation from method {0}\n port = {1} \n controller = {2} \n" + exception, _currentMethodName, _serialPort.PortName, _currentDoorNumber);
                readData.IsValid = false;
                readData.Buffer = new byte[0];
                return readData;
            }


            byte[] buffer = new byte[readBuffer.Length - 1];
            Array.Copy(readBuffer, buffer, readBuffer.Length - 1);

            if (GetCheckSum(buffer) == readBuffer[readBuffer.Length - 1])
            {
                readData.IsValid = true;
                readData.Buffer = buffer;
            }
            else
            {
                readData.IsValid = false;
                readData.Buffer = new byte[0];
            }


            return readData;
        }


        private byte _lastRespondedController;
        public byte GetNumberLastRespondedController()
        {
            return _lastRespondedController;
        }

        public bool IsOpen()
        {
            _logger.Info("serial port is open = " + _serialPort.IsOpen);
            return _serialPort.IsOpen;
        }

        public bool Open()
        {
            try
            {
                
                _serialPort.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
