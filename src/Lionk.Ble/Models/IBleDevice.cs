namespace Lionk.Ble.Models;

public interface IOnCharacteristicData
{
    void OnNewData(string uuid, byte[] data);
    void OnRegistered();
    void OnDisconnected();
}

public interface IBleDevice
{
    public event EventHandler? OnConnectionEstablished;
    public event EventHandler? OnSubscribeEstablished;
    public event EventHandler? OnDisconnected;
    public DeviceStatus Status { get; set; }
    public Task Connect();
    public string GetName();
    public string GetAddress();
    public Task<bool> IsConnected();
    public short GetRssi();

    public Task SubscribeToCharacteristic(
        string serviceId,
        string characteristicId,
        IOnCharacteristicData cb
    );
    public Task<byte[]> ReadCharacteristic(string serviceId, string characteristicId);
    public Task FetchProperties();
}
