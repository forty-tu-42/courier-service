using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Courier.Data;
using Courier.WPF.Dialogs;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class CouriersView : UserControl
{
    private DataTable? _current;

    public CouriersView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _current = ReferenceService.GetCouriers(SearchBox.Text);
            CouriersGrid.ItemsSource = _current.DefaultView;
            CountText.Text = $"Курьеров: {_current.Rows.Count}";
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Курьеры");
        }
    }

    private int? SelectedId
        => CouriersGrid.SelectedItem is DataRowView row ? Convert.ToInt32(row["id_kuriera"]) : null;

    private void Search_Click(object sender, RoutedEventArgs e) => LoadData();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoadData();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        LoadData();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CourierDialog(null) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        var dialog = new CourierDialog(id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void CouriersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => Edit_Click(sender, e);

    private void Archive_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        if (!Ui.Confirm("Переместить курьера в архив? Если у него есть активные заказы, операция будет отменена."))
            return;

        try
        {
            ReferenceService.ArchiveCourier(id);
            LoadData();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Архивирование курьера");
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, "Список курьеров");
    }
}
