using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Models;

public sealed record IndicatorMonthComparison(decimal? Change, bool LowerIsBetter)
{
    public string Text
    {
        get
        {
            if (Change is not decimal change) return "Sin datos para comparar";
            if (change == 0m) return "Sin cambio";
            var direction = change > 0m ? "Subió" : "Bajó";
            var amount = Math.Abs(change).ToString("0.00", OperationalDemoStore.HondurasCulture);
            return $"{direction} {amount} %";
        }
    }

    public string Color => Change switch
    {
        null or 0m => "#455D6F",
        > 0m => LowerIsBetter ? "#A52532" : "#24724F",
        _ => LowerIsBetter ? "#24724F" : "#A52532"
    };

    public static IndicatorMonthComparison Compare(decimal? previous, decimal? current, bool lowerIsBetter)
    {
        if (previous is not decimal baseline || current is not decimal latest)
            return new(null, lowerIsBetter);
        if (baseline == latest)
            return new(0m, lowerIsBetter);
        if (baseline == 0m)
            return new(null, lowerIsBetter);

        return new((latest - baseline) / Math.Abs(baseline) * 100m, lowerIsBetter);
    }
}
