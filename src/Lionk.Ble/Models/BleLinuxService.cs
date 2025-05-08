using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using Lionk.Core.Component;

namespace Lionk.Ble.Models;

public class BleLinuxService : BleService
{
    private Adapter _adapter;
    private Dictionary<string, IBleDevice> _connectedDevices = new();

    public BleLinuxService()
    {
        var taskAdapters = BlueZManager.GetAdaptersAsync();
        Adapter? adapter = taskAdapters.Result.FirstOrDefault();
        if (adapter is null)
        {
            throw new Exception("No Bluetooth adapter found");
        }
        _adapter = adapter;
    }

    async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
        var deviceProperties = await device.GetAllAsync();
        return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";
    }

    public async Task<byte[]> Read(string deviceId, string serviceId, string characteristicId)
    {
        var device = _connectedDevices[deviceId];
        return await device.ReadCharacteristic(serviceId, characteristicId);
    }
    public override async Task Subscribe(string deviceId, string serviceId, string characteristicId, IOnCharacteristicData cb)
    {
        var device = _connectedDevices[deviceId];
        await device.SubscribeToCharacteristic(serviceId, characteristicId, cb);
    }


    private async Task<IEnumerable<IBleDevice>> GetAvailableDevices()
    {
        int newDevices = 0;
        using (
            await _adapter.WatchDevicesAddedAsync(async device =>
            {
                newDevices++;
                // Write a message when we detect new devices during the scan.
                string deviceDescription = await GetDeviceDescriptionAsync(device);
                Console.WriteLine($"[NEW] {deviceDescription}");
            })
        )
        {
            await _adapter.StartDiscoveryAsync();
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _adapter.StopDiscoveryAsync();
        }
        /*var devices = await adapter.GetDevicesAsync();*/
        var result = new List<BleLinuxDevice>();
        /*return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";*/

        //await _adapter.StartDiscoveryAsync();
        IReadOnlyList<Device> devices = await _adapter.GetDevicesAsync();

        foreach (var dev in devices)
        {
            var deviceProperties = await dev.GetAllAsync();
            string deviceDescription = await GetDeviceDescriptionAsync(dev);
            Console.WriteLine($" - {deviceDescription}");
            result.Add(new(dev, deviceProperties));
        }

        return result;
    }

    public override async Task<IEnumerable<string?>> GetAvailableDevicesNames()
    {
        return (await GetAvailableDevices()).Select(x=> x.GetName());
    }

    public override async Task<DeviceStatus> RegisterDevice(string deviceName)
    {
        var devices = await GetAvailableDevices();
        Console.WriteLine($"Finding device {deviceName}");

        IBleDevice? device = (devices).FirstOrDefault(x =>
        {
                var name = x.GetName();
                Console.WriteLine("Checking device " + x.GetName());
                return deviceName.Equals(name);
            });
        if (device is null) return DeviceStatus.NotFound;
        device.OnConnectionEstablished += (sender, args) => _connectedDevices.Add(deviceName, device);
        device.OnDisconnected += (sender, args) => _connectedDevices.Remove(deviceName);

        Console.WriteLine($"Registring device {deviceName}");
        await device.Connect();
        return DeviceStatus.Connecting;
    }

    public override DeviceStatus GetDeviceStatus(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName)) return DeviceStatus.NotFound;
        if (_connectedDevices.TryGetValue(deviceName, out var device))
        {
            return device.Status;
        } else
        {
            return DeviceStatus.NotFound;
        }
    }
}
