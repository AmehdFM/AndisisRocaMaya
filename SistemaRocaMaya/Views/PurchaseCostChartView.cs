using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SistemaRocaMaya.Services.Reports;

namespace SistemaRocaMaya.Views;

public sealed class PurchaseCostChartView : FrameworkElement
{
    public static readonly DependencyProperty ChartProperty = DependencyProperty.Register(
        nameof(Chart), typeof(PurchaseCostChart), typeof(PurchaseCostChartView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public PurchaseCostChart? Chart
    {
        get => (PurchaseCostChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (Chart is not { } chart) return;

        const int left = PurchaseCostChart.Left, right = PurchaseCostChart.Right;
        const int top = PurchaseCostChart.Top, bottom = PurchaseCostChart.Bottom;
        var ink = Brushes.DarkSlateGray;
        var axes = new Pen(Brushes.SlateGray, 1);
        var grid = new Pen(Brushes.LightGray, 1);
        var line = new Pen(Brushes.SteelBlue, 2);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        void Text(string label, double x, double y, TextAlignment alignment = TextAlignment.Left)
        {
            var text = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, ink, dpi) { TextAlignment = alignment };
            context.DrawText(text, new Point(x, y));
        }

        Text("Costo unitario (L)", 8, 5);
        foreach (var tick in chart.CostTicks)
        {
            context.DrawLine(grid, new Point(left, tick.Position), new Point(right, tick.Position));
            Text(tick.Label, left - 8, tick.Position - 8, TextAlignment.Right);
        }
        context.DrawLine(axes, new Point(left, top), new Point(left, bottom));
        context.DrawLine(axes, new Point(left, bottom), new Point(right, bottom));
        foreach (var tick in chart.DateTicks)
        {
            context.DrawLine(axes, new Point(tick.Position, bottom), new Point(tick.Position, bottom + 5));
            Text(tick.Label, tick.Position, bottom + 6, TextAlignment.Center);
        }
        Text("Fecha de compra", (left + right) / 2, 211, TextAlignment.Center);

        for (var index = 1; index < chart.Points.Count; index++)
        {
            var previous = chart.Points[index - 1];
            var current = chart.Points[index];
            context.DrawLine(line, new Point(previous.X, previous.Y), new Point(current.X, current.Y));
        }
        foreach (var point in chart.Points)
            context.DrawEllipse(Brushes.SteelBlue, null, new Point(point.X, point.Y), 3, 3);
    }
}
