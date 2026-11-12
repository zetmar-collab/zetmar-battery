namespace Zetmar.Core;
public sealed record HistoryPoint(DateTimeOffset At, int? Percent, PowerState State);
public sealed class BatteryHistory
{
    private readonly List<HistoryPoint> points = new();
    public IReadOnlyList<HistoryPoint> Points => points;
    public void Load(IEnumerable<HistoryPoint> values,DateTimeOffset now)
    {
        points.Clear();points.AddRange(values.Where(p=>p!=null&&p.At>=now.AddDays(-7)&&p.At<=now&&(p.Percent==null||p.Percent is >=0 and <=100)&&Enum.IsDefined(p.State)).OrderBy(p=>p.At).TakeLast(20500));
    }
    public bool Prune(DateTimeOffset now) => points.RemoveAll(p=>p.At<now.AddDays(-7)||p.At>now)>0;
    public bool Add(BatteryReading reading,DateTimeOffset now)
    {
        Prune(now);
        if(points.Count>0&&now-points[^1].At<TimeSpan.FromSeconds(30))return false;
        points.Add(new(now,reading.Percent,reading.State));
        if(points.Count>20500)points.RemoveRange(0,points.Count-20500);
        return true;
    }
    public static (TimeSpan Observed,int Drop) Summary(IReadOnlyList<HistoryPoint> values)
    {
        double seconds=0;int drop=0;
        for(int i=1;i<values.Count;i++)
        {
            var a=values[i-1];var b=values[i];var duration=b.At-a.At;
            if(a.State!=PowerState.Battery||b.State!=PowerState.Battery||duration<=TimeSpan.Zero||duration>TimeSpan.FromMinutes(3))continue;
            seconds+=duration.TotalSeconds;
            if(a.Percent is int from&&b.Percent is int to&&from>=to)drop+=from-to;
        }
        return(TimeSpan.FromSeconds(seconds),drop);
    }
}
