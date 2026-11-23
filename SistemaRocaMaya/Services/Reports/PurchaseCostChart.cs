using System.Globalization;
using System.Security;

namespace SistemaRocaMaya.Services.Reports;

public sealed class PurchaseCostChart
{
    public const int Width = 760;
    public const int Height = 230;
    public const int Left = 92;
    public const int Right = 710;
    public const int Top = 39;
    public const int Bottom = 180;

    public IReadOnlyList<ChartPoint> Points { get; }
    public IReadOnlyList<ChartTick> DateTicks { get; }
    public IReadOnlyList<ChartTick> CostTicks { get; }

    public PurchaseCostChart(DateTime start, DateTime end, IReadOnlyList<(DateTime Date, decimal Cost)> purchases)
    {
        var days = (end.Date - start.Date).TotalDays;
        var margin = Math.Max(1, Math.Ceiling(days * 0.05));
        var span = days + margin * 2;
        var maxCost = Math.Max(1m, purchases.Max(purchase => purchase.Cost) * 1.1m);
        double X(double offset) => Left + (offset + margin) / span * (Right - Left);
        double Y(decimal cost) => Bottom - (double)(cost / maxCost) * (Bottom - Top);

        Points = purchases.Select(purchase => new ChartPoint(
            X((purchase.Date.Date - start.Date).TotalDays), Y(purchase.Cost))).ToArray();
        DateTicks = Enumerable.Range(0, 5).Select(index => Math.Round(-margin + span * index / 4))
            .Distinct().Where(offset => offset >= (DateTime.MinValue - start.Date).TotalDays
                && offset <= (DateTime.MaxValue.Date - start.Date).TotalDays)
            .Select(offset => new ChartTick(X(offset), start.Date.AddDays(offset).ToString("dd/MM/yyyy"))).ToArray();
        CostTicks = Enumerable.Range(0, 5).Select(index => maxCost * index / 4)
            .Select(cost => new ChartTick(Y(cost), OperationalDemoStore.FormatMoney(cost))).ToArray();
    }

    public string RenderSvg()
    {
        static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        var grid = string.Concat(CostTicks.Select(tick =>
            $"<path d=\"M {Left} {N(tick.Position)} H {Right}\" stroke=\"#D8E3EB\"/>" +
            $"<text x=\"{Left - 8}\" y=\"{N(tick.Position + 4)}\" text-anchor=\"end\">{SecurityElement.Escape(tick.Label)}</text>"));
        var dates = string.Concat(DateTicks.Select(tick =>
            $"<path d=\"M {N(tick.Position)} {Bottom} v 5\" stroke=\"#64748B\"/>" +
            $"<text x=\"{N(tick.Position)}\" y=\"{Bottom + 19}\" text-anchor=\"middle\">{tick.Label}</text>"));
        var coordinates = string.Join(" ", Points.Select(point => $"{N(point.X)},{N(point.Y)}"));
        var markers = string.Concat(Points.Select(point => $"<circle cx=\"{N(point.X)}\" cy=\"{N(point.Y)}\" r=\"3\" fill=\"#245F8C\"/>"));
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {Width} {Height}" font-family="Lato" font-size="12" fill="#1E293B">
            <text x="8" y="19" font-weight="700">Costo unitario (L)</text>
            {grid}
            <path d="M {Left} {Top} V {Bottom} H {Right}" fill="none" stroke="#64748B"/>
            {dates}
            <polyline points="{coordinates}" fill="none" stroke="#245F8C" stroke-width="2"/>
            {markers}
            <text x="{(Left + Right) / 2}" y="222" text-anchor="middle">Fecha de compra</text>
            </svg>
            """;
    }

    public sealed record ChartPoint(double X, double Y);
    public sealed record ChartTick(double Position, string Label);
}
