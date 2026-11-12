using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
namespace Zetmar.Battery;
public static class UiTheme
{
    public static void Apply(Window window, App app)
    {
        if(window.Resources.MergedDictionaries.Count==0)
        {
            using var stream=typeof(UiTheme).Assembly.GetManifestResourceStream("Zetmar.UiStyles.xaml")??throw new InvalidOperationException("Missing UI styles");
            window.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
        }
        Brush Color(string light,string dark)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(app.Dark?dark:light));
        var r=window.Resources;
        r["CanvasBrush"]=app.Surface;r["TextBrush"]=app.Ink;r["AccentBrush"]=app.Accent;
        r["CardBrush"]=SystemParameters.HighContrast?SystemColors.WindowBrush:Color("#FFFFFF","#1D2834");
        r["SecondaryTextBrush"]=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Color("#536879","#B4C4CF");
        r["CardBorderBrush"]=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Color("#D9E2E8","#3C4E5D");
        r["ControlBrush"]=SystemParameters.HighContrast?SystemColors.WindowBrush:Color("#F5F7F9","#263440");
        r["OutlineBrush"]=SystemParameters.HighContrast?SystemColors.WindowTextBrush:Color("#7B8E9B","#6C8192");
        r["HoverBrush"]=SystemParameters.HighContrast?SystemColors.WindowBrush:Color("#E7EEF2","#344857");
        r["SelectionTextBrush"]=SystemParameters.HighContrast?SystemColors.HighlightTextBrush:app.Accent;
        r["AccentSoftBrush"]=SystemParameters.HighContrast?SystemColors.HighlightBrush:Color("#E0F3EC","#203F37");
        r["OnAccentBrush"]=SystemParameters.HighContrast?SystemColors.HighlightTextBrush:Color("#FFFFFF","#102A22");
        r["AccentHoverBrush"]=SystemParameters.HighContrast?SystemColors.HighlightBrush:Color("#005640","#A2F0D5");
        ApplyTitleBar(window,app.Dark);
    }
    public static void ApplyTitleBar(Window window,bool dark)
    {
        var handle=new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if(handle==IntPtr.Zero)return;
        int value=dark?1:0;Native.DwmSetWindowAttribute(handle,20,ref value,sizeof(int));
        int rounded=2;Native.DwmSetWindowAttribute(handle,33,ref rounded,sizeof(int));
        int caption=-1,text=-1;
        if(!SystemParameters.HighContrast)
        {
            Color background=(Color)ColorConverter.ConvertFromString(dark?"#1D2834":"#FFFFFF");
            Color foreground=(Color)ColorConverter.ConvertFromString(dark?"#F0F6FA":"#152B37");
            caption=background.R|(background.G<<8)|(background.B<<16);text=foreground.R|(foreground.G<<8)|(foreground.B<<16);
        }
        Native.DwmSetWindowAttribute(handle,35,ref caption,sizeof(int));Native.DwmSetWindowAttribute(handle,36,ref text,sizeof(int));
    }
    public static double Contrast(Brush background,Brush foreground)
    {
        static double L(Color c)
        {
            static double S(byte b){double v=b/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
            return .2126*S(c.R)+.7152*S(c.G)+.0722*S(c.B);
        }
        if(background is not SolidColorBrush bg||foreground is not SolidColorBrush fg)return 21;
        double a=L(bg.Color),b=L(fg.Color);return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);
    }
    public static Brush Brush(FrameworkElement element,string key)=>(Brush)element.FindResource(key);
}
