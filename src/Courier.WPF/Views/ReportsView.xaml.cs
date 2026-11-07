using System.Data;
using System.Windows;
using System.Windows.Controls;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class ReportsView : UserControl
{
    private readonly int? _courierFilter;
    private DataTable? _current;

    public ReportsView(int? courierFilter = null)
    {
        InitializeComponent();
        _courierFilter = courierFilter;
        DateFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTo.SelectedDate = DateTime.Today;
        Loaded += (_, _) => BuildReport();
    }

    private string SelectedReport =>
        (ReportTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "deliveries";

    private void Build_Click(object sender, RoutedEventArgs e) => BuildReport();

    private void BuildReport()
    {
        try
        {
            var from = DateFrom.SelectedDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var to = DateTo.SelectedDate ?? DateTime.Today;
            if (to < from)
            {
                Ui.Warn("Дата окончания периода не может быть раньше даты начала.");
                return;
            }

            // Курьеру доступны только отчёты по доставкам и загрузке.
            if (_courierFilter is not null)
            {
                var allowed = SelectedReport is "deliveries" or "couriers";
                if (!allowed)
                {
                    ReportTypeBox.SelectedIndex = 0;
                }
            }

            _current = SelectedReport switch
            {
                "deliveries" => ReportService.GetDeliveries(from, to),
                "statuses" => ReportService.GetOrdersByStatus(from, to),
                "couriers" => ReportService.GetCourierLoad(),
                "zones" => ReportService.GetRevenueByZone(from, to),
                "months" => ReportService.GetMonthlyRevenue(from, to),
                _ => ReportService.GetDeliveries(from, to)
            };

            ReportGrid.ItemsSource = _current.DefaultView;
            ReportTitle.Text = (ReportTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Отчёт";
            ReportSubtitle.Text = $"Период: {from:dd.MM.yyyy} — {to:dd.MM.yyyy}";

            ConfigureColumns();
            SummaryText.Text = BuildSummary();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Построение отчёта");
        }
    }

    private void ConfigureColumns()
    {
        if (_current is null) return;

        ReportGrid.Columns.Clear();
        foreach (DataColumn column in _current.Columns)
        {
            var header = column.ColumnName switch
            {
                "nomer" => "Номер",
                "klient" => "Клиент",
                "kurier" => "Курьер",
                "zona" => "Зона",
                "ves" => "Вес, кг",
                "cena" => "Сумма",
                "sposob_oplaty" => "Оплата",
                "data_sozdaniya" => "Создан",
                "data_dostavki" => "Доставлен",
                "status" => "Статус",
                "kolichestvo" => "Количество",
                "summa" => "Сумма",
                "fio" => "Курьер",
                "transport" => "Транспорт",
                "vsego_zakazov" => "Всего",
                "dostavleno" => "Доставлено",
                "v_rabote" => "В работе",
                "summa_dostavki" => "Сумма доставок",
                "zakazov" => "Заказов",
                "vyruchka" => "Выручка",
                "sredniy_chek" => "Средний чек",
                "period" => "Период",
                _ => column.ColumnName
            };

            var binding = new System.Windows.Data.Binding(column.ColumnName);
            if (column.DataType == typeof(DateTime))
                binding.StringFormat = "{0:dd.MM.yyyy HH:mm}";
            else if (column.DataType == typeof(decimal) || column.DataType == typeof(double))
                binding.StringFormat = "{0:N2}";

            var width = column.ColumnName switch
            {
                "nomer" => 150.0,
                "kurier" => 160.0,
                "zona" => 110.0,
                "ves" => 75.0,
                "cena" => 100.0,
                "sposob_oplaty" => 90.0,
                "data_sozdaniya" => 130.0,
                "data_dostavki" => 140.0,
                "status" => 130.0,
                "period" => 110.0,
                _ => 0.0
            };

            ReportGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = binding,
                Width = width > 0
                    ? new DataGridLength(width)
                    : new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }

    private string BuildSummary()
    {
        if (_current is null) return "";

        decimal sum = 0;
        foreach (DataRow row in _current.Rows)
        {
            foreach (var name in new[] { "cena", "summa", "summa_dostavki", "vyruchka" })
            {
                if (_current.Columns.Contains(name) && row[name] is not DBNull)
                {
                    sum += Convert.ToDecimal(row[name]);
                    break;
                }
            }
        }

        return $"Записей: {_current.Rows.Count}   ·   Итого: {sum:N2} ₽";
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, ReportTitle.Text);
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.ExportCsv(_current, ReportTitle.Text);
    }
}
