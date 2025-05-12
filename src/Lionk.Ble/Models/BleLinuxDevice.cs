using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;

namespace Lionk.Ble.Models;
class BleLinuxDevice : IBleDevice
{
    private Device _device;
    private Device1Properties _props;
    private static readonly Dictionary<string, IBleCallback> _subscribers = new();

    /// <inheritdoc/>
    public event EventHandler? OnConnectionEstablished;

    /// <inheritdoc/>
    public event EventHandler? OnSubscribeEstablished;

    /// <inheritdoc/>
    public event EventHandler? OnDisconnected;
    public BleLinuxDevice(Device device, Device1Properties props)
    {
        device.Connected += ConnectedAsync;
        device.Disconnected += DisconnectedAsync;
        device.ServicesResolved += ServicesResolvedAsync;
        this._device = device;
        this._props = props;
    }

    /// <inheritdoc/>
    public DeviceStatus Status { get; set; }

    /// <inheritdoc/>
    public string GetName()
    {
        return string.IsNullOrEmpty(_props.Name) ? string.Empty : _props.Name;
    }

    /// <inheritdoc/>
    public string GetAddress()
    {
        return string.IsNullOrEmpty(_props.Address) ? string.Empty : _props.Address;
    }

    /// <inheritdoc/>
    public short GetRssi()
    {
        return _props.RSSI;
    }

    /// <inheritdoc/>
    public async Task SubscribeToCharacteristic(
        string serviceId,
        string characteristicId,
        IBleCallback onData
    )
    {
        IGattService1 service = await _device.GetServiceAsync(serviceId);
        if (service is null)
        {
            Console.WriteLine($"Couldn't read service {serviceId}");
            return;
        }
        GattCharacteristic characteristic = await service.GetCharacteristicAsync(characteristicId);

        if (characteristic is null)
        {
            Console.WriteLine($"Couldn't read characteristic {characteristicId}");
            return;
        }
        string uuid = await characteristic.GetUUIDAsync();
        if (_subscribers.TryGetValue(uuid, out var subscriber))
        {
            Console.WriteLine($"Already subscribed to {uuid}");
            return;
        }
        _subscribers.Add(uuid, onData);
        await characteristic.StartNotifyAsync();
        OnSubscribeEstablished?.Invoke(this, new BlueZEventArgs(false));
        characteristic.Value += OnNotification;
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadCharacteristic(string serviceId, string characteristicId)
    {
        IGattService1 service = await _device.GetServiceAsync(serviceId);
        if (service == null)
        {
            Console.WriteLine($"Couldn't read service {serviceId}");
            return [];
        }
        GattCharacteristic characteristic = await service.GetCharacteristicAsync(characteristicId);
        if (characteristic == null)
        {
            Console.WriteLine($"Couldn't read characteristic {characteristicId}");
            return [];
        }

        return await characteristic.ReadValueAsync(TimeSpan.FromSeconds(15));
    }

    /// <inheritdoc/>
    public async Task FetchProperties()
    {
        _props = await _device.GetAllAsync();
    }


    /// <inheritdoc/>
    public async Task<bool> IsConnected()
    {
        return await _device.GetConnectedAsync();
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        bool isAlreadyConnected = await IsConnected();
        if (isAlreadyConnected)
        {
            Console.WriteLine("Already connected");
            OnConnectionEstablished?.Invoke(this, new BlueZEventArgs(false));
            return;
        }
        Status = DeviceStatus.Connecting;
        await _device.ConnectAsync();
    }

    /// <inheritdoc/>
    public Task Disconnect()
    {
        Status = DeviceStatus.Disconnecting;
        _device.DisconnectAsync();
        return Task.CompletedTask;
    }

    private Task ConnectedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        OnConnectionEstablished?.Invoke(this, eventArgs);
        Status = DeviceStatus.Connected;
        return Task.CompletedTask;
    }

    private Task DisconnectedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        OnDisconnected?.Invoke(sender, eventArgs);
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    private Task ServicesResolvedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        Status = DeviceStatus.Ready;
        return Task.CompletedTask;
    }

    private static async Task OnNotification(
        GattCharacteristic characteristic,
        GattCharacteristicValueEventArgs e
    )
    {
        var uuid = await characteristic.GetUUIDAsync();
        var data = e.Value;
        if (_subscribers.TryGetValue(uuid, out var subscriber))
        {
            subscriber.OnNotify(uuid, data);
        }
        else
        {
            Console.WriteLine("Couldn't find characteristic subscriber");
        }
    }
}
