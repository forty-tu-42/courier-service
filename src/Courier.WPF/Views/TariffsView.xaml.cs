using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Courier.Data;
using Courier.WPF.Dialogs;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class TariffsView : UserControl
{
    private DataTable? _current;

    public TariffsView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            if (ZoneFilter.ItemsSource is null)
            {
                ZoneFilter.ItemsSource = ReferenceService.GetZones().DefaultView;
                ZoneFilter.SelectedIndex = -1;
            }

            int? zoneId = ZoneFilter.SelectedValue is int id ? id : null;
            _current = ReferenceService.GetTariffs(zoneId);
            TariffsGrid.ItemsSource = _current.DefaultView;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Тарифы");
        }
    }

    private int? SelectedId
        => TariffsGrid.SelectedItem is DataRowView row ? Convert.ToInt32(row["id_tarifa"]) : null;

    private void ZoneFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadData();

    private void AllZones_Click(object sender, RoutedEventArgs e)
    {
        ZoneFilter.SelectedIndex = -1;
        LoadData();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TariffDialog(null) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        var dialog = new TariffDialog(id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void TariffsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => Edit_Click(sender, e);

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        if (!Ui.Confirm("Удалить выбранный тариф?")) return;

        try
        {
            ReferenceService.DeleteTariff(id);
            LoadData();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Удаление тарифа");
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, "Тарифы курьерской службы");
    }
}
