using Lionk.Core.Component;

namespace Lionk.Ble.Models;

public abstract class BleService : BaseCyclicComponent

{
    private const int BleServicePeriod = 5;
    public abstract List<IBleDevice> GetDevices();
    public abstract void RegisterDevice(string deviceAddress, IOnCharacteristicData cb);
    public abstract void Subscribe(string deviceId, string serviceId, string characteristicId, IOnCharacteristicData cb);
    public abstract string GetDeviceName(string deviceAddress);
    public abstract short? GetRssiOfDevice(string deviceAddress);
    public abstract DeviceStatus GetDeviceStatus(string? deviceAddress);

    protected BleService()
    {
        Period = TimeSpan.FromSeconds(5);
    }
}
