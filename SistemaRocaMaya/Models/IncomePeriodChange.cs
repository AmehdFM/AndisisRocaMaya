using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Models;

public sealed record IncomePeriodChange(string Label, decimal? Percent, bool? IsFavorable)
{
    public string Text => Percent is decimal value
        ? $"{value.ToString("+0.00;-0.00;0.00", OperationalDemoStore.HondurasCulture)} %"
        : "Sin base de comparación";

    public static IncomePeriodChange Compare(string label, decimal first, decimal last, bool lowerIsBetter = false)
    {
        var change = last - first;
        if (change == 0m) return new(label, 0m, null);
        if (first == 0m) return new(label, null, null);

        var percent = decimal.Round(change / Math.Abs(first) * 100m, 2);
        return new(label, percent, percent == 0m ? null : lowerIsBetter ? percent < 0m : percent > 0m);
    }
}
