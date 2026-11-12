namespace Zetmar.Core;

public enum PowerState { Unavailable, NoBattery, Unknown, Battery, Charging, Adapter, Full }
public record BatteryReading(PowerState State, int? Percent = null, uint? Seconds = null);
public static class BatteryMapper
{
    public static BatteryReading Map(bool success, byte ac, byte flags, byte percent, uint seconds)
    {
        if (!success) return new(PowerState.Unavailable);
        if (flags != 255 && (flags & 128) != 0) return new(PowerState.NoBattery);
        int? p = percent <= 100 ? percent : null;
        if (flags == 255) return new(PowerState.Unknown, p);
        if ((flags & 8) != 0) return new(PowerState.Charging, p);
        if (ac == 1) return new(p == 100 ? PowerState.Full : PowerState.Adapter, p);
        if (ac != 0) return new(PowerState.Unknown, p);
        return new(PowerState.Battery, p, seconds == uint.MaxValue ? null : seconds);
    }
}
public sealed class DischargeEstimator
{
    private readonly List<(DateTimeOffset Time, int Percent)> samples = [];
    public int Count => samples.Count;
    public TimeSpan Observed => samples.Count < 2 ? TimeSpan.Zero : samples[^1].Time - samples[0].Time;
    public int PercentageDrop => samples.Count < 2 ? 0 : Math.Max(0, samples[0].Percent - samples[^1].Percent);
    public void Reset() => samples.Clear();
    public TimeSpan? Update(BatteryReading r, DateTimeOffset now)
    {
        if (r.State != PowerState.Battery || r.Percent is not int p) { Reset(); return null; }
        if (samples.Count > 0)
        {
            var last = samples[^1];
            var elapsed = now - last.Time;
            if (elapsed <= TimeSpan.Zero || elapsed > TimeSpan.FromMinutes(3) || p > last.Percent || last.Percent - p > 3) Reset();
        }
        samples.Add((now, p));
        while (samples.Count > 1 && now - samples[0].Time > TimeSpan.FromMinutes(45)) samples.RemoveAt(0);
        if (samples.Count < 3 || now - samples[0].Time < TimeSpan.FromMinutes(10) || samples[0].Percent - p < 2) return null;
        // Linear regression smooths quantized 1% readings across the observation window.
        double meanX = samples.Average(s => (s.Time - samples[0].Time).TotalHours);
        double meanY = samples.Average(s => s.Percent);
        double numerator = 0, denominator = 0;
        foreach (var s in samples) { double x = (s.Time - samples[0].Time).TotalHours - meanX; numerator += x * (s.Percent - meanY); denominator += x * x; }
        double rate = denominator > 0 ? -numerator / denominator : 0;
        if (rate < 0.5 || rate > 100) return null;
        return TimeSpan.FromHours(Math.Clamp(p / rate, 0, 200));
    }
}
public static class BatteryText
{
    public static string Duration(TimeSpan time, bool pl, bool compact = false)
    {
        int minutes = Math.Max(0, (int)Math.Floor(time.TotalMinutes));
        if (compact) return $"~{minutes / 60}:{minutes % 60:00}";
        if (minutes < 1) return pl ? "~<1 min" : "~<1 min";
        return minutes >= 60 ? $"~{minutes / 60} {Localization.Get("godz.",pl)} {minutes % 60} min" : $"~{minutes} min";
    }
    public static string State(PowerState s, bool pl) => Localization.Get(s switch
    {
        PowerState.Unavailable => "Dane niedostępne", PowerState.NoBattery => "Brak baterii",
        PowerState.Charging => "Ładowanie", PowerState.Adapter => "Zasilacz", PowerState.Full => "Naładowana",
        PowerState.Battery => "Praca na baterii", _ => "Stan nieznany"
    },pl);
    public static string Summary(BatteryReading r, TimeSpan? remaining, bool estimate, bool pl, bool compact)
    {
        if (r.State is PowerState.NoBattery or PowerState.Unavailable) return State(r.State, pl);
        string tail = r.State == PowerState.Battery ? remaining is { } t ? Duration(t, pl, compact) : estimate ? Localization.Get("Obliczanie…",pl) : Localization.Get("Czas niedostępny",pl) : State(r.State, pl);
        return $"{(r.Percent is int p ? p + "%" : "—")} · {tail}";
    }
}
