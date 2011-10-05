using System.ServiceModel;

namespace AlfaServer.Services
{
    public interface IClientServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AlertAboutControllerNotResponsible(string portName, byte controllerNumber);

        [OperationContract(IsOneWay = true)]
        void AlertAboutControllerBeganRespond(string portName, byte controllerNumber);

        [OperationContract(IsOneWay = true)]
        void AlertComPortNotResponsible(string portName);

        [OperationContract(IsOneWay = true)]
        void AlertComPortBeganRespond(string portName);
        
        [OperationContract(IsOneWay = true)]
        void AlertGerkon(long roomId, byte keyNumber, bool alarm);

        [OperationContract(IsOneWay = true)]
        void AlertUnsetKey(string portName, byte controllerNumber);

        [OperationContract(IsOneWay = true)]
        void AlertChangeRoomsOnTheFloor(string portName);

        [OperationContract(IsOneWay = true)]
        void AlertChangeFloors(string portName);

        [OperationContract(IsOneWay = true)]
        void ReloadRooms(string portName, long floorId);
    }
}
