using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class AuditView : UserControl
{
    private DataTable? _current;

    public AuditView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _current = ReferenceService.GetAudit(SearchBox.Text);
            AuditGrid.ItemsSource = _current.DefaultView;
            CountText.Text = $"Записей: {_current.Rows.Count}";
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Журнал аудита");
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e) => LoadData();
    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoadData();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, "Журнал аудита");
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.ExportCsv(_current, "Журнал_аудита");
    }
}
