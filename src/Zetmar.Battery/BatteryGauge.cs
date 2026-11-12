using System;
using System.Windows;
using System.Windows.Media;
using Zetmar.Core;
namespace Zetmar.Battery;
// Vector geometry keeps the battery sharp at every widget size and DPI.
public sealed class BatteryGauge : FrameworkElement
{
    private BatteryReading reading = new(PowerState.Unavailable);
    private Brush ink = Brushes.White;
    public void Update(BatteryReading value, Brush foreground)
    {
        reading = value; ink = foreground; InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.PushTransform(new ScaleTransform(ActualWidth / 44, ActualHeight / 24));
        var pen = new Pen(ink, 1.8);
        dc.DrawRoundedRectangle(null, pen, new Rect(1, 2, 37, 20), 3, 3);
        dc.DrawRoundedRectangle(ink, null, new Rect(39.5, 8, 3.5, 8), 1, 1);
        bool unavailable = reading.State is PowerState.NoBattery or PowerState.Unavailable;
        if (!unavailable && reading.Percent is int percent && percent > 0)
            dc.DrawRoundedRectangle(ink, null, new Rect(4, 5, 31 * percent / 100d, 14), 1, 1);
        if (unavailable)
        {
            dc.DrawLine(pen, new Point(13, 7), new Point(25, 17));
            dc.DrawLine(pen, new Point(25, 7), new Point(13, 17));
        }
        else if (reading.Percent == null)
        {
            dc.DrawEllipse(ink, null, new Point(13, 12), 1.3, 1.3);
            dc.DrawEllipse(ink, null, new Point(19, 12), 1.3, 1.3);
            dc.DrawEllipse(ink, null, new Point(25, 12), 1.3, 1.3);
        }
        if (reading.State == PowerState.Charging)
        {
            // Dual stroke keeps the charging mark visible over empty and filled areas.
            var bolt = Geometry.Parse("M24,3 L16,13 L21,13 L18,21 L28,10 L22,10 Z");
            Brush opposite = ink is SolidColorBrush solid ? WidgetColors.Contrasting(solid.Color) : Brushes.Black;
            dc.DrawGeometry(opposite, new Pen(ink, 2.5), bolt);
        }
        dc.Pop();
    }
}
