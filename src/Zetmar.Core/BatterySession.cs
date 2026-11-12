namespace Zetmar.Core;
public enum BatteryAlert { None, Low, ChargeTarget }
public sealed class BatterySession
{
    public DateTimeOffset? Started { get; private set; }
    private bool lowWarned, chargeWarned;
    public void Resume() => Started = null;
    public BatteryAlert Update(BatteryReading reading, DateTimeOffset now, bool lowEnabled, int lowThreshold, bool chargeEnabled = false, int chargeThreshold = 80)
    {
        if (!lowEnabled) lowWarned = false;
        if (!chargeEnabled) chargeWarned = false;
        if (reading.State is PowerState.Unavailable or PowerState.Unknown) return BatteryAlert.None;
        if (reading.State == PowerState.NoBattery) { Started = null; lowWarned = chargeWarned = false; return BatteryAlert.None; }
        if (reading.State == PowerState.Battery)
        {
            Started ??= now;
            chargeWarned = false;
            if (reading.Percent is not int p) return BatteryAlert.None;
            if (p > lowThreshold + 3) lowWarned = false;
            if (lowEnabled && !lowWarned && p <= lowThreshold) { lowWarned = true; return BatteryAlert.Low; }
        }
        else
        {
            Started = null; lowWarned = false;
            if (reading.Percent is not int p) return BatteryAlert.None;
            if (p < chargeThreshold - 3) chargeWarned = false;
            if (chargeEnabled && !chargeWarned && p >= chargeThreshold) { chargeWarned = true; return BatteryAlert.ChargeTarget; }
        }
        return BatteryAlert.None;
    }
}
