namespace Lionk.Ble.Models;

public interface IBatterySensor
{
    /// <summary>
    /// Method to get the voltage of the battery.
    /// </summary>
    /// <param name="nbDecimal"> Number of decimal to display</param>
    /// <returns> The voltage of the battery</returns>
    public double GetVoltage(int nbDecimal = 1);

    /// <summary>
    /// Method to get the percentage of the battery.
    /// </summary>
    /// <param name="nbDecimal"> Number of decimal to display</param>
    /// <returns> The percentage of the battery</returns>
    public double GetPercentage(int nbDecimal = 0);

    /// <summary>
    /// Method that indicates if the battery is low voltage.
    /// </summary>
    /// <returns> True if the battery is low voltage, false otherwise</returns>
    public bool IsLowVoltage();
}
