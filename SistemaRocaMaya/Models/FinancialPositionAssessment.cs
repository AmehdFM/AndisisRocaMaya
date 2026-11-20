using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Models;

public sealed record FinancialPositionAssessment(decimal Assets, decimal Liabilities)
{
    public decimal? ExternalFinancingPercent => Assets > 0m && Liabilities >= 0m
        ? Liabilities / Assets * 100m : null;

    public string RecommendationColor => ExternalFinancingPercent switch
    {
        <= 30m => "#86EFAC",
        <= 60m => "#FDBA74",
        > 60m => "#FCA5A5",
        _ when Assets == 0m && Liabilities > 0m => "#FCA5A5",
        _ => "#CBD5E1"
    };

    public string Recommendation
    {
        get
        {
            if (Assets == 0m && Liabilities > 0m)
                return "La empresa tiene deudas y no registra activos en este corte. Prioriza la reducción de pasivos y revisa la liquidez.";

            if (ExternalFinancingPercent is not decimal percentage)
                return "No se puede evaluar el financiamiento externo con los saldos de este corte.";

            var result = percentage switch
            {
                <= 30m => "Está bien. Mantener este nivel de financiamiento externo y vigilar la capacidad de pago.",
                <= 60m => "Alto pero saludable. Controlar los vencimientos y el flujo de efectivo antes de asumir nuevas deudas.",
                <= 100m => "Muy alto. Priorizar la reducción de pasivos y revisar la capacidad de pago antes de asumir nuevas deudas.",
                _ => "La empresa tiene más deudas que bienes. Priorizar la reducción de pasivos y revisar la liquidez."
            };
            return $"El {percentage.ToString("0.00", OperationalDemoStore.HondurasCulture)} % de los activos está financiado externamente. {result}";
        }
    }
}
