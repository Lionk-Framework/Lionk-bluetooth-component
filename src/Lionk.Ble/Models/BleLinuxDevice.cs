using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;

namespace Lionk.Ble.Models;

class CharacteristicNotificationData : ICharacteristicNotificationData
{
    private string _uuid;
    private byte[] _data;

    public CharacteristicNotificationData(string uuid, byte[] data)
    {
        this._uuid = uuid;
        this._data = data;
    }

    public string GetUuid()
    {
        return _uuid;
    }

    public byte[] GetValue()
    {
        return _data;
    }
}

class BleLinuxDevice : IBleDevice
{
    private Device _device;
    private Device1Properties _props;
    private static readonly Dictionary<string, IOnCharacteristicData> _subscribers = new();

    public event EventHandler? OnConnectionEstablished;
    public event EventHandler? OnSubscribeEstablished;
    public event EventHandler? OnDisconnected;
    public BleLinuxDevice(Device device, Device1Properties props)
    {
        device.Connected += ConnectedAsync;
        device.Disconnected += DisconnectedAsync;
        device.ServicesResolved += ServicesResolvedAsync;
        this._device = device;
        this._props = props;
    }

    public DeviceStatus Status { get; set; }

    public string GetName()
    {
        if (_props.Name is null)
        {
            return string.Empty;
        }
        return _props.Name;
    }

    public string GetAddress()
    {
        if (_props.Address is null)
        {
            return string.Empty;
        }
        return _props.Address;
    }

    public short GetRssi()
    {
        return _props.RSSI;
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
            var notificationData = new CharacteristicNotificationData(uuid, data);
            subscriber.OnNewData(notificationData);
        }
        else
        {
            Console.WriteLine("Couldn't find characteristic subscriber");
        }
    }

    public async Task SubscribeToCharacteristic(
        string serviceId,
        string characteristicId,
        IOnCharacteristicData onData
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

    public async Task FetchProperties()
    {
        _props = await _device.GetAllAsync();
    }

    public async Task<bool> IsConnected()
    {
        return await _device.GetConnectedAsync();
    }

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

    public Task Disconnect()
    {
        Status = DeviceStatus.Disconnecting;
        _device.DisconnectAsync();
        return Task.CompletedTask;
    }

    Task ConnectedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        OnConnectionEstablished?.Invoke(this, eventArgs);
        Status = DeviceStatus.Connected;
        return Task.CompletedTask;
    }

    Task DisconnectedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        OnDisconnected?.Invoke(sender, eventArgs);
        Status = DeviceStatus.Disconnected;
        return Task.CompletedTask;
    }

    Task ServicesResolvedAsync(Device sender, BlueZEventArgs eventArgs)
    {
        Status = DeviceStatus.Ready;
        return Task.CompletedTask;
    }
}
