namespace Lionk.Ble.Models;

public interface IBleCallback
{
    /// <summary>
    /// The callback to be called when a new data is received.
    /// </summary>
    /// <param name="uuid"> The uuid of the characteristic</param>
    /// <param name="data"> The data received from the characteristic</param>
    void OnNotify(string uuid, byte[] data);

    /// <summary>
    /// The callback to be called when the device is registered.
    /// </summary>
    void OnRegistered();

    /// <summary>
    /// The callback to be called when the device is disconnected.
    /// </summary>
    void OnDisconnected();
}

public interface IBleDevice
{
    /// <summary>
    /// Event raised when the connection is established.
    /// </summary>
    public event EventHandler? OnConnectionEstablished;

    /// <summary>
    /// Event raised when the subscription is established.
    /// </summary>
    public event EventHandler? OnSubscribeEstablished;

    /// <summary>
    /// Event raised when the device is disconnected.
    /// </summary>
    public event EventHandler? OnDisconnected;

    /// <summary>
    /// Property that indicate the status of the device.
    /// </summary>
    public DeviceStatus Status { get; }

    /// <summary>
    /// Method to connect to the device.
    /// </summary>
    /// <returns></returns>
    public Task Connect();

    /// <summary>
    /// Method to disconnect the device.
    /// </summary>
    /// <returns></returns>
    public Task Disconnect();

    /// <summary>
    /// Methode to get the name of the device.
    /// </summary>
    /// <returns> The name of the device. </returns>
    public string GetName();

    /// <summary>
    /// Methode to get the address of the device.
    /// </summary>
    /// <returns> The address of the device. </returns>
    public string GetAddress();

    /// <summary>
    /// Method that indicate if the device is connected.
    /// </summary>
    /// <returns> True if the device is connected, false otherwise. </returns>
    public Task<bool> IsConnected();

    /// <summary>
    /// Method to get the rssi of the device.
    /// </summary>
    /// <returns> The rssi of the device. </returns>
    public short GetRssi();

    /// <summary>
    /// Method to subscribe to a characteristic.
    /// </summary>
    /// <param name="serviceId"> The id of the service. </param>
    /// <param name="characteristicId"> The id of the characteristic. </param>
    /// <param name="callBackImplementation"> The callback implementation. </param>
    /// <returns> The task to subscribe to the characteristic. </returns>
    public Task SubscribeToCharacteristic(
        string serviceId,
        string characteristicId,
        IBleCallback callBackImplementation
    );

    /// <summary>
    /// Method to read a readable characteristic.
    /// </summary>
    /// <param name="serviceId"> The id of the service. </param>
    /// <param name="characteristicId"> The id of the characteristic. </param>
    /// <returns> The task that contains a byte[] with the data read. </returns>
    public Task<byte[]> ReadCharacteristic(string serviceId, string characteristicId);

    /// <summary>
    /// Methode to fetch the properties of the device.
    /// </summary>
    /// <returns> The task to fetch the properties of the device. </returns>
    /// <remarks> It will be called before asking for the properties like rssi, name, address, etc.</remarks>
    public Task FetchProperties();
}
