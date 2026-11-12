using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Zetmar.Core;
namespace Zetmar.Battery;
public sealed class HistoryWindow : Window
{
    private readonly App app;
    private Action? draw;
    private sealed record SampleRow(string At,string Percent,string State);
    public HistoryWindow(App app,HistoryStore store)
    {
        this.app=app;Title=app.T("Historia baterii");Width=Math.Min(820,SystemParameters.WorkArea.Width-32);Height=Math.Min(660,SystemParameters.WorkArea.Height-40);MinWidth=Math.Min(440,Width);MinHeight=Math.Min(400,Height);
        FontFamily=new FontFamily("Segoe UI");FontSize=14;UseLayoutRounding=true;SnapsToDevicePixels=true;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        UiTheme.Apply(this,app);Background=app.Surface;Foreground=app.Ink;
        SourceInitialized+=(_,_)=>UiTheme.ApplyTitleBar(this,app.Dark);
        var root=new StackPanel { Margin=new Thickness(24) };Content=new ScrollViewer { Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
        TextBlock Text(string value,double size=13)
        {
            var block=new TextBlock { Text=value,FontSize=size,TextWrapping=TextWrapping.Wrap,LineHeight=size*1.45 };
            block.SetResourceReference(TextBlock.ForegroundProperty,size>14?"TextBrush":"SecondaryTextBrush");return block;
        }
        var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var heading=Text(Title,26);heading.FontWeight=FontWeights.SemiBold;header.Children.Add(heading);
        var range=new ComboBox { ItemsSource=new[]{"24 h",app.T("7 dni")},SelectedIndex=0,Width=120,VerticalAlignment=VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(range,app.T("Zakres historii"));Grid.SetColumn(range,1);header.Children.Add(range);root.Children.Add(header);
        var intro=Text(app.T("Pomiary zapisane na tym komputerze. Przerwy w odczytach nie są łączone."));intro.Margin=new Thickness(0,8,0,20);root.Children.Add(intro);
        var statsGrid=new System.Windows.Controls.Primitives.UniformGrid { Columns=3,Margin=new Thickness(0,0,0,16) };root.Children.Add(statsGrid);
        TextBlock Stat(string label)
        {
            var content=new StackPanel();var caption=Text(label,12);caption.Margin=new Thickness(0,0,0,6);content.Children.Add(caption);var value=Text("—",18);value.FontWeight=FontWeights.SemiBold;content.Children.Add(value);
            var card=new Border { Child=content,CornerRadius=new CornerRadius(10),Padding=new Thickness(14),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,8,0) };card.SetResourceReference(Border.BackgroundProperty,"CardBrush");card.SetResourceReference(Border.BorderBrushProperty,"CardBorderBrush");statsGrid.Children.Add(card);return value;
        }
        var observed=Stat(app.T("Praca na baterii"));var used=Stat(app.T("Zużycie"));var count=Stat(app.T("Liczba pomiarów"));
        var chart=new Canvas { Height=260,ClipToBounds=true };
        var plot=new StackPanel();plot.Children.Add(chart);
        var empty=Text("");empty.Margin=new Thickness(0,12,0,0);plot.Children.Add(empty);
        var dates=Text("",12);dates.Margin=new Thickness(0,14,0,0);plot.Children.Add(dates);
        var frame=new Border { Child=plot,Padding=new Thickness(16),CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1) };frame.SetResourceReference(Border.BackgroundProperty,"CardBrush");frame.SetResourceReference(Border.BorderBrushProperty,"CardBorderBrush");root.Children.Add(frame);
        var legend=new WrapPanel { Margin=new Thickness(0,16,0,16) };
        void Legend(string label,bool dashed)
        {
            var row=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,24,0) };
            var line=new Line { X1=0,X2=24,Y1=8,Y2=8,StrokeThickness=2,Width=24,Height=16,Margin=new Thickness(0,0,8,0) };line.SetResourceReference(Shape.StrokeProperty,dashed?"TextBrush":"AccentBrush");if(dashed)line.StrokeDashArray=new DoubleCollection{4,3};row.Children.Add(line);row.Children.Add(Text(label,12));legend.Children.Add(row);
        }
        Legend(app.T("Praca na baterii"),false);Legend(app.T("Zasilacz / ładowanie"),true);root.Children.Add(legend);
        var table=new DataGrid { IsReadOnly=true,AutoGenerateColumns=false,CanUserAddRows=false,MaxHeight=240,EnableRowVirtualization=true,SelectionMode=DataGridSelectionMode.Single };
        table.Columns.Add(new DataGridTextColumn { Header=app.T("Data i godzina"),Binding=new Binding("At"),Width=new DataGridLength(2,DataGridLengthUnitType.Star) });
        table.Columns.Add(new DataGridTextColumn { Header=app.T("Naładowanie"),Binding=new Binding("Percent"),Width=new DataGridLength(1,DataGridLengthUnitType.Star) });
        table.Columns.Add(new DataGridTextColumn { Header=app.T("Stan"),Binding=new Binding("State"),Width=new DataGridLength(2,DataGridLengthUnitType.Star) });
        var details=new Expander { Header=app.T("Ostatnie 100 pomiarów"),Content=table,Margin=new Thickness(0,0,0,8) };root.Children.Add(details);
        HistoryPoint[] visible=Array.Empty<HistoryPoint>();int tooltipIndex=-1;
        void Draw()
        {
            chart.Children.Clear();var end=DateTimeOffset.UtcNow;var start=range.SelectedIndex==0?end.AddHours(-24):end.AddDays(-7);
            visible=store.Data.Points.Where(p=>p.At>=start&&p.At<=end).ToArray();tooltipIndex=-1;
            var summary=BatteryHistory.Summary(visible);observed.Text=BatteryText.Duration(summary.Observed,app.Polish).TrimStart('~');used.Text=summary.Drop+app.T(" p.p.");count.Text=visible.Length.ToString();
            empty.Text=visible.Count(p=>p.Percent!=null)<2?app.T("Za mało danych. Historia zbiera próbki tylko przy włączonej opcji."):"";
            if(store.Error!=null)empty.Text+="\n"+app.T("Nie udało się zapisać historii.");empty.Visibility=empty.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
            double width=Math.Max(1,chart.ActualWidth-48),height=220;
            foreach(int percent in new[]{0,25,50,75,100})
            {
                double y=10+height*(100-percent)/100;
                chart.Children.Add(new Line { X1=40,Y1=y,X2=width+40,Y2=y,Stroke=UiTheme.Brush(this,"CardBorderBrush") });
                var label=Text(percent+"%",12);Canvas.SetTop(label,y-8);chart.Children.Add(label);
            }
            Polyline? segment=null;HistoryPoint? last=null;
            foreach(var p in visible)
            {
                if(p.Percent==null){segment=null;last=null;continue;}
                if(segment==null||last==null||p.At-last.At>TimeSpan.FromMinutes(3)||p.State!=last.State)
                {
                    segment=new Polyline { Stroke=p.State==PowerState.Battery?app.Accent:app.Ink,StrokeThickness=2 };
                    if(p.State!=PowerState.Battery)segment.StrokeDashArray=new DoubleCollection {4,3};chart.Children.Add(segment);
                }
                segment.Points.Add(new Point(40+(p.At-start).TotalSeconds/(end-start).TotalSeconds*width,10+height*(100-p.Percent.Value)/100));last=p;
            }
            dates.Text=$"{start.LocalDateTime:g}  →  {end.LocalDateTime:g}";
            if(details.IsExpanded)table.ItemsSource=visible.TakeLast(100).Reverse().Select(p=>new SampleRow(p.At.LocalDateTime.ToString("g"),p.Percent is int level?level+"%":"—",BatteryText.State(p.State,app.Polish))).ToArray();
        }
        chart.MouseMove+=(_,e)=>
        {
            if(visible.Length==0)return;double ratio=Math.Clamp((e.GetPosition(chart).X-40)/Math.Max(1,chart.ActualWidth-48),0,1);
            var end=DateTimeOffset.UtcNow;var start=range.SelectedIndex==0?end.AddHours(-24):end.AddDays(-7);var target=start+(end-start)*ratio;
            int lo=0,hi=visible.Length-1;while(lo<hi){int mid=(lo+hi)/2;if(visible[mid].At<target)lo=mid+1;else hi=mid;}
            if(lo>0&&(target-visible[lo-1].At).Duration()<(visible[lo].At-target).Duration())lo--;
            if(lo==tooltipIndex)return;tooltipIndex=lo;var p=visible[lo];chart.ToolTip=$"{p.At.LocalDateTime:g} · {(p.Percent is int value?value+"%":"—")} · {BatteryText.State(p.State,app.Polish)}";
        };
        draw=Draw;range.SelectionChanged+=(_,_)=>Draw();chart.SizeChanged+=(_,_)=>Draw();details.Expanded+=(_,_)=>Draw();Loaded+=(_,_)=>Draw();
    }
    public void RefreshTheme()
    {
        UiTheme.Apply(this,app);Background=app.Surface;Foreground=app.Ink;draw?.Invoke();
    }
}
