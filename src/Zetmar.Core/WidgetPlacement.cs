namespace Zetmar.Core;
public readonly record struct ScreenArea(int Left,int Top,int Right,int Bottom);
public readonly record struct WidgetPoint(int X,int Y);
public static class WidgetPlacement
{
    public static WidgetPoint Place(ScreenArea a,int width,int height,int margin,string anchor,int? x=null,int? y=null)
    {
        int maxX=Math.Max(a.Left+margin,a.Right-width-margin), maxY=Math.Max(a.Top+margin,a.Bottom-height-margin);
        int px=anchor.Contains("left")?a.Left+margin:maxX;
        int py=anchor.StartsWith("top")?a.Top+margin:maxY;
        if(anchor=="free") { px=x??maxX; py=y??maxY; }
        return new(Math.Clamp(px,a.Left+margin,maxX),Math.Clamp(py,a.Top+margin,maxY));
    }
    public static WidgetPoint Snap(ScreenArea a,int width,int height,int margin,int x,int y,int distance)
    {
        int left=a.Left+margin,right=a.Right-width-margin,top=a.Top+margin,bottom=a.Bottom-height-margin;
        if(Math.Abs((long)x-left)<=distance)x=left;else if(Math.Abs((long)x-right)<=distance)x=right;
        if(Math.Abs((long)y-top)<=distance)y=top;else if(Math.Abs((long)y-bottom)<=distance)y=bottom;
        return Place(a,width,height,margin,"free",x,y);
    }
    public static ScreenArea AvoidBar(ScreenArea work,ScreenArea monitor,int edge,int thickness) => edge switch
    {
        0=>work with { Left=Math.Max(work.Left,monitor.Left+thickness) },
        1=>work with { Top=Math.Max(work.Top,monitor.Top+thickness) },
        2=>work with { Right=Math.Min(work.Right,monitor.Right-thickness) },
        3=>work with { Bottom=Math.Min(work.Bottom,monitor.Bottom-thickness) },
        _=>work
    };
}
