using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using Lionk.Core;

namespace Lionk.Ble.Models;

[NamedElement("BleLinux", "A Ble service for Linux")]
public class BleLinuxService : BleService
{
    private Adapter? _adapter;
    private Dictionary<string, IBleDevice> _connectedDevices = new();
    private List<DeviceToSubscribe> _deviceToSubscribe = new();
    private List<DeviceToRegister> _devicesToRegister = new();
    private List<IBleDevice> _devices = new();
    private bool _isDiscovering;
    
    /// <inheritdoc/>
    public override bool CanExecute { get; } = true;
    
    /// <inheritdoc/>
    protected override void OnExecute(CancellationToken cancellationToken)
    {
        if (_adapter is null)
        {
            _ = CreateAsync();
        }
        else
        {
            _ = GetAvailableDevices();
            GetAllProperties();
            RegisterAllDevices();
            SubscribeAllDevices();
        }

        base.OnExecute(cancellationToken);
    }
    
    /// <inheritdoc/>
    public override void RegisterDevice(string deviceAddress, IBleCallback callbackImplementation)
    {
        _devicesToRegister.Add(new DeviceToRegister(deviceAddress, callbackImplementation));
    }

    /// <inheritdoc/>
    public override void Subscribe(string deviceAddress, string serviceId, string characteristicId,
        IBleCallback callbackImplementation)
    {
        _deviceToSubscribe.Add(new DeviceToSubscribe(deviceAddress, serviceId, characteristicId, callbackImplementation));
    }

    /// <inheritdoc/>
    public override async Task<byte[]> Read(string deviceId, string serviceId, string characteristicId)
    {
        var device = _connectedDevices[deviceId];
        return await device.ReadCharacteristic(serviceId, characteristicId);
    }

    /// <inheritdoc/>
    public override string GetDeviceName(string deviceAddress)
    {
        if (_connectedDevices.TryGetValue(deviceAddress, out var device))
        {
            return device.GetName();
        }
        else
        {
            return string.Empty;
        }
    }

    /// <inheritdoc/>
    public override short? GetRssiOfDevice(string deviceAddress)
    {
        if (_connectedDevices.TryGetValue(deviceAddress, out var device))
        {
            return device.GetRssi();
        }
        else return null;
    }

    /// <inheritdoc/>
    public override List<IBleDevice> GetDevices()
    {
        return _devices;
    }

    /// <inheritdoc/>
    public override DeviceStatus GetDeviceStatus(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName)) return DeviceStatus.NotFound;
        if (_connectedDevices.TryGetValue(deviceName, out var device))
        {
            return device.Status;
        }
        else
        {
            return DeviceStatus.NotFound;
        }
    }

    private async Task CreateAsync()
    {
        var adapters = await BlueZManager.GetAdaptersAsync();
        Adapter? adapter = adapters.FirstOrDefault();
        if (adapter is null)
        {
            throw new Exception("No Bluetooth adapter found");
        }

        _adapter = adapter;
        await GetAvailableDevices();
    }

    private void GetAllProperties()
    {
        foreach (var device in _connectedDevices)
        {
            device.Value.FetchProperties();
        }
    }

    private void SubscribeAllDevices()
    {
        var devicesToIterate = new List<DeviceToSubscribe>(_deviceToSubscribe);
        foreach (var deviceToSubscribe in devicesToIterate)
        {
            SubscribeDevice(deviceToSubscribe.Address, deviceToSubscribe.ServiceId,
                deviceToSubscribe.CharacteristicId, deviceToSubscribe.Callback);
        }
    }

    private void SubscribeDevice(string deviceId, string serviceId, string characteristicId,
        IBleCallback cb)
    {
        if (_connectedDevices.TryGetValue(deviceId, out var device))
        {
            Console.WriteLine($"[SUB] Subscribing to characteristic {characteristicId}");
            device.SubscribeToCharacteristic(serviceId, characteristicId, cb).GetAwaiter().GetResult();
            return;
        }

        Console.WriteLine($"[SUB] Couldn't find device {deviceId}");
    }

    private void RemoveDeviceToSubscribe(string deviceAddress)
    {
        var deviceToSubscribe = _deviceToSubscribe.FirstOrDefault(x => x.Address == deviceAddress);
        if (deviceToSubscribe != null)
        {
            _deviceToSubscribe.Remove(deviceToSubscribe);
        }
    }

    private void RegisterAllDevices()
    {
        List<DeviceToRegister> registeredDevices = new();
        foreach (var deviceToRegister in _devicesToRegister)
        {
            if (RegisterDevice(deviceToRegister) != DeviceStatus.NotFound)
            {
                registeredDevices.Add(deviceToRegister);
            }
        }

        foreach (var deviceToRegister in registeredDevices)
        {
            _devicesToRegister.Remove(deviceToRegister);
        }
    }

    private DeviceStatus RegisterDevice(DeviceToRegister deviceToRegister)
    {
        IBleDevice? device = GetDevice(deviceToRegister.Address);
        if (device is null) return DeviceStatus.NotFound;

        Console.WriteLine($"Found {deviceToRegister.Address} as {device.GetName()}");
        device.OnConnectionEstablished += (sender, args) =>
        {
            _connectedDevices.TryAdd(deviceToRegister.Address, device);
            deviceToRegister.Callback.OnRegistered();
        };
        device.OnDisconnected += (sender, args) =>
        {
            _connectedDevices.Remove(deviceToRegister.Address);
            deviceToRegister.Callback.OnDisconnected();
        };

        device.OnSubscribeEstablished += (sender, args) =>
        { Console.WriteLine("Correctly subscribed");
            RemoveDeviceToSubscribe(deviceToRegister.Address);
        };
        Console.WriteLine($"Connecting to device {device.GetName()}");
        _ = device.Connect();
        return device.Status;
    }

    private async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
        var deviceProperties = await device.GetAllAsync();
        return $"{deviceProperties.Alias} (Address: {deviceProperties.Address}, RSSI: {deviceProperties.RSSI})";
    }

    private IBleDevice? GetDevice(string deviceAddress)
    {
        IEnumerable<IBleDevice> devices = _devices;
        var device = devices.FirstOrDefault(x => deviceAddress.Equals(x.GetAddress()));
        return device;
    }

    private async Task GetAvailableDevices()
    {
        if (_isDiscovering)
            return;

        _isDiscovering = true;

        try
        {
            if (_adapter is null)
            {
                Console.WriteLine("[GET DEVICES] No Bluetooth adapter found");
                return;
            }

            int newDevices = 0;
            using (
                await _adapter.WatchDevicesAddedAsync(async void (device) =>
                {
                    newDevices++;
                    string deviceDescription = await GetDeviceDescriptionAsync(device);
                    // Console.WriteLine($"[NEW] {deviceDescription}");
                })
            )
            {
                await _adapter.StartDiscoveryAsync();
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _adapter.StopDiscoveryAsync();
                // Console.WriteLine($"[NEW] {newDevices} devices discovered");
            }

            List<BleLinuxDevice> result = [];
            IReadOnlyList<Device> devices = await _adapter.GetDevicesAsync();

            foreach (var dev in devices)
            {
                var deviceProperties = await dev.GetAllAsync();
                result.Add(new(dev, deviceProperties));
            }

            _devices = result.Cast<IBleDevice>().ToList();
        }
        finally
        {
            _isDiscovering = false;
        }
    }
}

internal class DeviceToRegister
{
    public string Address { get; set; }
    public IBleCallback Callback { get; set; }

    public DeviceToRegister(string address, IBleCallback callback)
    {
        Address = address;
        Callback = callback;
    }
}

internal class DeviceToSubscribe
{
    public string Address { get; set; }
    public string ServiceId { get; set; }
    public string CharacteristicId { get; set; }
    public IBleCallback Callback { get; set; }

    public DeviceToSubscribe(string address, string serviceId, string characteristicId, IBleCallback callback)
    {
        Address = address;
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
        Callback = callback;
    }
}