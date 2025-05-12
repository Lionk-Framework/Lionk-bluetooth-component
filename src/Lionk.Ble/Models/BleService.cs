using Lionk.Core.Component;

namespace Lionk.Ble.Models;

public abstract class BleService : BaseCyclicComponent

{
    private const int BleServicePeriod = 5;

    /// <summary>
    /// Method to get devices
    /// </summary>
    /// <returns> List of devices</returns>
    public abstract List<IBleDevice> GetDevices();
    
    /// <summary>
    /// Method to register a device
    /// </summary>
    /// <param name="deviceAddress"> The address of the device</param>
    /// <param name="callbackImplementation"> The callback to be called when the device is registered</param>
    public abstract void RegisterDevice(string deviceAddress, IBleCallback callbackImplementation);
    
    /// <summary>
    /// The methode to subscribe to a device to a characteristic
    /// </summary>
    /// <param name="deviceAddress"> The address of the device</param>
    /// <param name="serviceId"> The service id of the device</param>
    /// <param name="characteristicId"> The characteristic id of the device</param>
    /// <param name="callbackImplementation"> The callback to be called when the device is subscribed</param>
    public abstract void Subscribe(string deviceAddress, string serviceId, string characteristicId, IBleCallback callbackImplementation);

    /// <summary>
    /// Method to get the name of the device by its address
    /// </summary>
    /// <param name="deviceAddress"> The address of the device</param>
    /// <returns> The name of the device</returns>
    public abstract string GetDeviceName(string deviceAddress);

    /// <summary>
    /// Method to get the RSSI of the device
    /// </summary>
    /// <param name="deviceAddress"> The address of the device</param>
    /// <returns> The RSSI of the device</returns>
    public abstract short? GetRssiOfDevice(string deviceAddress);

    /// <summary>
    /// Method to get the status of the device
    /// </summary>
    /// <param name="deviceAddress"> The address of the device</param>
    /// <returns> The status of the device</returns>
    public abstract DeviceStatus GetDeviceStatus(string? deviceAddress);

    /// <summary>
    /// Default constructor that sets the initial period of the cyclic component
    /// </summary>
    protected BleService()
    {
        Period = TimeSpan.FromSeconds(5);
    }
}
