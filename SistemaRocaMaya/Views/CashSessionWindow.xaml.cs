using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Views;

public partial class CashSessionWindow : Window
{
    private readonly CashCountViewModel _viewModel;

    public IReadOnlyList<CashDenominationCount> CashCounts => _viewModel.Rows
        .Where(row => row.Quantity > 0)
        .Select(row => new CashDenominationCount(row.Denomination, row.Quantity))
        .ToArray();

    public CashSessionWindow(bool isClosing, decimal expectedAmount = 0m)
    {
        InitializeComponent();
        _viewModel = new CashCountViewModel(isClosing, expectedAmount);
        DataContext = _viewModel;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

public sealed class CashCountViewModel : ObservableObject
{
    private readonly decimal _expectedAmount;
    private decimal _total;

    public ObservableCollection<CashDenominationEntryViewModel> Rows { get; } = [];
    public string TitleText { get; }
    public string ActionLabel { get; }
    public Visibility ExpectedPanelVisibility { get; }
    public string ExpectedAmountText => OperationalDemoStore.FormatMoney(_expectedAmount);
    public string TotalText => OperationalDemoStore.FormatMoney(_total);

    public CashCountViewModel(bool isClosing, decimal expectedAmount)
    {
        _expectedAmount = expectedAmount;
        TitleText = isClosing ? "Cerrar caja" : "Abrir caja";
        ActionLabel = isClosing ? "Guardar cierre" : "Abrir caja";
        ExpectedPanelVisibility = isClosing ? Visibility.Visible : Visibility.Collapsed;
        foreach (var denomination in OperationalDemoStore.CashDenominations)
        {
            var row = new CashDenominationEntryViewModel(denomination);
            row.PropertyChanged += Row_PropertyChanged;
            Rows.Add(row);
        }
    }

    private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _total = Rows.Sum(row => row.Subtotal);
        OnPropertyChanged(nameof(TotalText));
    }
}

public sealed class CashDenominationEntryViewModel(decimal denomination) : ObservableObject
{
    private string _quantityText = "0";

    public decimal Denomination { get; } = denomination;
    public string DenominationText => $"L {Denomination:0.00}";
    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetProperty(ref _quantityText, value))
            {
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(SubtotalText));
            }
        }
    }
    public int Quantity => int.TryParse(QuantityText, out var quantity) && quantity > 0 ? quantity : 0;
    public decimal Subtotal => Denomination * Quantity;
    public string SubtotalText => OperationalDemoStore.FormatMoney(Subtotal);
}
