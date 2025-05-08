using Lionk.Core.Component;

namespace Lionk.Ble.Models;

public abstract class BleService : BaseComponent

{
    public abstract Task<IEnumerable<string?>> GetAvailableDevicesNames();
    public abstract Task<DeviceStatus> RegisterDevice(string deviceName);
    public abstract Task Subscribe(string deviceId, string serviceId, string characteristicId, IOnCharacteristicData cb);


    public abstract DeviceStatus GetDeviceStatus(string? deviceName);
}
