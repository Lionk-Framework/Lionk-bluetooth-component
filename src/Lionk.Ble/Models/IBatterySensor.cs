namespace Lionk.Ble.Models;

public interface IBatterySensor
{
    double GetVoltage();
    double GetPercentage();
    bool IsLowVoltage();
}
