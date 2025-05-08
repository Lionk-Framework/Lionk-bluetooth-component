namespace Lionk.Ble.Models;

public interface ICharacteristicNotificationData
{
    string GetUuid();
    byte[] GetValue();
}

public interface IOnCharacteristicData
{
    Task OnNewData(ICharacteristicNotificationData data);
}

public interface IBleDevice
{
    public event EventHandler? OnConnectionEstablished;
    public event EventHandler? OnDisconnected;
    public DeviceStatus Status { get; }
    public Task Connect();
    public string? GetName();
    public Task<bool> IsConnected();

    public Task SubscribeToCharacteristic(
        string serviceId,
        string characteristicId,
        IOnCharacteristicData cb
    );
    public Task<byte[]> ReadCharacteristic(string serviceId, string characteristicId);
}
