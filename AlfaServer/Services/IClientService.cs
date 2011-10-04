using System;
using System.ServiceModel;

namespace AlfaServer.Services
{
    [ServiceContract (
        SessionMode = SessionMode.Required,
        CallbackContract = typeof(IClientServiceCallback)
    )]
    public interface IClientService
    {
        //ключ, номер в ячейке, этаж, контроллер, тип ключа, фио пользователя, дата окончания срока действия ключа
        [OperationContract]
        bool SetKey(byte[] key, byte number, string portName, byte controllerNumber, string name, byte type, DateTime endDate);

        //этаж, номер контроллера, номер ячейки
        [OperationContract]
        bool UnsetKey(string portName, byte controllerNumber, byte number);

        [OperationContract]
        byte[] ReadKey(string portName);

        [OperationContract]
        bool SetRoomToProtect(string portName, byte controllerNumber, bool isProtected);

        [OperationContract]
        bool SetLight(string portName, byte controllerNumber, bool lightOn);

        [OperationContract]
        bool Join(string portName);

        [OperationContract]
        bool SetMasterKey(byte[] key);

        [OperationContract]
        bool AddRoomToFloor(string portName, int roomNumber, string roomClass, byte controllerNumber,
            bool onLine, int roomCategory, bool isProtected);

        [OperationContract]
        bool AddFloor(string portName, string floorName);

        [OperationContract]
        bool StopFloorPolling(string portName);

        [OperationContract]
        bool StartFloorPolling(string portName);

        [OperationContract]
        bool SetDataBaseConnectionString(string name, string ip, string login, string password);

        [OperationContract]
        bool Ping();

        [OperationContract(IsOneWay = true)]
        void SetAllRoomToProtect(string portName, bool isProtected);

        [OperationContract(IsOneWay = true)]
        void SetAllRoomLight(string portName, bool lightOn);
    }
}
