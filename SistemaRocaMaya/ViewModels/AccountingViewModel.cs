using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SistemaRocaMaya.ViewModels;

public sealed class AccountingViewModel : ObservableObject
{
    private static readonly HashSet<string> AccountingSections = new(StringComparer.Ordinal)
    {
        "Resumen contable",
        "Libro diario",
        "Libro mayor",
        "Kardex PEPS",
        "Estado de situación financiera",
        "Estado de resultados"
    };

    private string _selectedSection = "Resumen contable";

    public string SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                OnPropertyChanged(nameof(SummaryVisibility));
                OnPropertyChanged(nameof(JournalVisibility));
                OnPropertyChanged(nameof(LedgerVisibility));
                OnPropertyChanged(nameof(KardexVisibility));
                OnPropertyChanged(nameof(FinancialPositionVisibility));
                OnPropertyChanged(nameof(IncomeStatementVisibility));
            }
        }
    }

    public string PeriodLabel => "Octubre 2026";

    public string KardexProductLabel => "Arroz 1 libra";
    public string KardexClosingStock => "6 unidades";

    public Visibility SummaryVisibility => VisibilityFor("Resumen contable");
    public Visibility JournalVisibility => VisibilityFor("Libro diario");
    public Visibility LedgerVisibility => VisibilityFor("Libro mayor");
    public Visibility KardexVisibility => VisibilityFor("Kardex PEPS");
    public Visibility FinancialPositionVisibility => VisibilityFor("Estado de situación financiera");
    public Visibility IncomeStatementVisibility => VisibilityFor("Estado de resultados");

    public ObservableCollection<AccountingMetricViewModel> SummaryMetrics { get; } =
    [
        new("INGRESOS", "L 38,400.00", ""),
        new("COSTOS Y GASTOS", "L 25,120.00", ""),
        new("RESULTADO NETO", "L 13,280.00", ""),
        new("ACTIVOS", "L 52,900.00", "")
    ];

    public ObservableCollection<AccountingMetricViewModel> FinancialPositionMetrics { get; } =
    [
        new("ACTIVOS", "L 52,900.00", ""),
        new("PASIVOS", "L 7,600.00", ""),
        new("PATRIMONIO", "L 45,300.00", "")
    ];

    public ObservableCollection<AccountingMetricViewModel> IncomeMetrics { get; } =
    [
        new("INGRESOS", "L 38,400.00", ""),
        new("COSTOS Y GASTOS", "L 25,120.00", ""),
        new("RESULTADO NETO", "L 13,280.00", "")
    ];

    public ObservableCollection<AccountingPeriodViewModel> PeriodPerformance { get; } =
    [
        new("May", 58, 39),
        new("Jun", 66, 43),
        new("Jul", 62, 46),
        new("Ago", 78, 52),
        new("Sep", 91, 67),
        new("Oct", 108, 72)
    ];

    public ObservableCollection<JournalEntryViewModel> JournalEntries { get; } =
    [
        new("25 oct 2026", "V-0421", "Venta de contado", "Caja", "L 250.00", "—"),
        new("25 oct 2026", "V-0421", "Venta de contado", "Ingresos por ventas", "—", "L 250.00"),
        new("25 oct 2026", "C-0018", "Venta a crédito", "Crédito por cobrar", "L 180.00", "—"),
        new("25 oct 2026", "C-0018", "Venta a crédito", "Ingresos por ventas", "—", "L 180.00"),
        new("24 oct 2026", "E-0102", "Entrada de mercadería", "Inventario", "L 1,200.00", "—"),
        new("24 oct 2026", "E-0102", "Entrada de mercadería", "Proveedores", "—", "L 1,200.00")
    ];

    public ObservableCollection<LedgerEntryViewModel> LedgerEntries { get; } =
    [
        new("01 oct 2026", "Saldo inicial", "—", "—", "L 8,400.00"),
        new("02 oct 2026", "Venta V-0398", "L 320.00", "—", "L 8,720.00"),
        new("03 oct 2026", "Compra E-0094", "—", "L 1,150.00", "L 7,570.00"),
        new("05 oct 2026", "Abono de cliente", "L 200.00", "—", "L 7,770.00"),
        new("25 oct 2026", "Venta V-0421", "L 250.00", "—", "L 8,020.00")
    ];

    public ObservableCollection<KardexMovementViewModel> KardexMovements { get; } =
    [
        new("02 oct 2026", "Entrada · compra E-0094", "L-2401", "12", "—", "12"),
        new("04 oct 2026", "Entrada · compra E-0098", "L-2402", "8", "—", "20"),
        new("07 oct 2026", "Salida · venta V-0402", "L-2401", "—", "10", "10"),
        new("09 oct 2026", "Salida · venta V-0409", "L-2401", "—", "2", "8"),
        new("09 oct 2026", "Salida · venta V-0409", "L-2402", "—", "2", "6")
    ];

    public ObservableCollection<StatementLineViewModel> FinancialPositionLines { get; } =
    [
        new("ACTIVOS", "", "", false, true),
        new("", "Caja", "L 12,400.00", false),
        new("", "Inventario", "L 37,200.00", false),
        new("", "Crédito por cobrar", "L 3,300.00", false),
        new("TOTAL ACTIVOS", "", "L 52,900.00", true),
        new("PASIVOS", "", "", false, true),
        new("", "Proveedores", "L 7,600.00", false),
        new("TOTAL PASIVOS", "", "L 7,600.00", true),
        new("PATRIMONIO", "", "", false, true),
        new("", "Capital y resultados acumulados", "L 45,300.00", false),
        new("TOTAL PASIVOS Y PATRIMONIO", "", "L 52,900.00", true)
    ];

    public ObservableCollection<StatementLineViewModel> IncomeStatementLines { get; } =
    [
        new("INGRESOS", "", "", false, true),
        new("", "Ventas netas", "L 38,400.00", false),
        new("COSTOS DE VENTAS", "", "", false, true),
        new("", "Costo de mercadería vendida", "L 18,900.00", false),
        new("UTILIDAD BRUTA", "Ingresos menos costo de ventas", "L 19,500.00", true),
        new("GASTOS OPERATIVOS", "", "", false, true),
        new("", "Gastos del período", "L 6,220.00", false),
        new("RESULTADO NETO", "Utilidad del período", "L 13,280.00", true)
    ];

    public static bool IsAccountingSection(string title) => AccountingSections.Contains(title);

    public void SelectSection(string title)
    {
        if (IsAccountingSection(title))
        {
            SelectedSection = title;
        }
    }

    private Visibility VisibilityFor(string title) => SelectedSection == title ? Visibility.Visible : Visibility.Collapsed;
}

public sealed record AccountingMetricViewModel(string Label, string Value, string Detail);

public sealed record JournalEntryViewModel(string Date, string Reference, string Description, string Account, string Debit, string Credit);

public sealed record LedgerEntryViewModel(string Date, string Description, string Debit, string Credit, string Balance);

public sealed record KardexMovementViewModel(string Date, string Description, string Lot, string Entry, string Exit, string Balance);

public sealed record StatementLineViewModel(string Group, string Account, string Amount, bool IsTotal, bool IsSectionHeader = false);

public sealed record AccountingPeriodViewModel(string Month, double SalesBarHeight, double CostsBarHeight);
