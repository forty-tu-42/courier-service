using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Courier.Data;
using Courier.WPF.Dialogs;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class OrdersView : UserControl
{
    private readonly int? _courierFilter;
    private DataRowView? _selected;
    private DataTable? _current;

    public OrdersView(int? courierFilter = null)
    {
        InitializeComponent();
        _courierFilter = courierFilter;

        DateFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DateTo.SelectedDate = DateTime.Today;

        LoadStatuses();
        ApplyPermissions();
        Loaded += (_, _) => LoadOrders();
    }

    private void LoadStatuses()
    {
        try
        {
            StatusFilter.ItemsSource = ReferenceService.GetStatuses().DefaultView;
            StatusFilter.SelectedIndex = -1;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Статусы");
        }
    }

    private void ApplyPermissions()
    {
        var user = AppSession.User;
        var canEdit = user.CanEditOrders;
        NewButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        EditButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        AssignButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

        EditButton.IsEnabled = AssignButton.IsEnabled = CancelButton.IsEnabled = StatusButton.IsEnabled = false;
    }

    public void LoadOrders()
    {
        try
        {
            int? statusId = StatusFilter.SelectedValue is int id ? id : null;
            _current = OrderService.GetOrders(
                SearchBox.Text,
                statusId,
                DateFrom.SelectedDate,
                DateTo.SelectedDate,
                _courierFilter);

            OrdersGrid.ItemsSource = _current.DefaultView;
            CountText.Text = $"Заказов: {_current.Rows.Count}";
            _selected = null;
            ClearDetails();
            UpdateButtons();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Загрузка заказов");
        }
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = OrdersGrid.SelectedItem as DataRowView;
        UpdateButtons();
        ShowDetails();
    }

    private void OrdersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_selected is null) return;
        if (AppSession.User.CanEditOrders) EditOrder();
    }

    private void UpdateButtons()
    {
        var has = _selected is not null;
        var canEdit = AppSession.User.CanEditOrders;

        EditButton.IsEnabled = has && canEdit;
        AssignButton.IsEnabled = has && canEdit;
        CancelButton.IsEnabled = has && canEdit;
        StatusButton.IsEnabled = has;
    }

    private void ShowDetails()
    {
        if (_selected is null)
        {
            ClearDetails();
            return;
        }

        var row = _selected;
        DetailNumber.Text = row["nomer"]?.ToString() ?? "—";
        DetailStatus.Text = row["status"]?.ToString() ?? "";
        DetailClient.Text = row["klient"]?.ToString() ?? "—";
        DetailClientPhone.Text = row["telefon_klienta"]?.ToString() ?? "";
        DetailSender.Text = row["adres_otpravitelya"]?.ToString() ?? "—";
        DetailRecipient.Text = row["fio_poluchatelya"]?.ToString() ?? "—";
        DetailRecipientPhone.Text = row["telefon_poluchatelya"]?.ToString() ?? "";
        DetailRecipientAddress.Text = row["adres_poluchatelya"]?.ToString() ?? "—";
        DetailZone.Text = row["zona"]?.ToString() ?? "—";
        DetailWeight.Text = row["ves"] is DBNull ? "—" : $"{Convert.ToDecimal(row["ves"]):N2} кг";
        DetailPrice.Text = row["cena"] is DBNull ? "—" : $"{Convert.ToDecimal(row["cena"]):N2} ₽";
        DetailPayment.Text = row["sposob_oplaty"] is DBNull ? "—" : row["sposob_oplaty"]?.ToString();
        DetailCourier.Text = row["kurier"] is DBNull ? "не назначен" : row["kurier"]?.ToString();
        DetailComment.Text = row["kommentariy"] is DBNull ? "—" : row["kommentariy"]?.ToString();

        try
        {
            var history = OrderService.GetHistory(Convert.ToInt32(row["id_zakaza"]));
            HistoryList.ItemsSource = history.DefaultView;
        }
        catch
        {
            HistoryList.ItemsSource = null;
        }
    }

    private void ClearDetails()
    {
        DetailNumber.Text = "Выберите заказ";
        DetailStatus.Text = "";
        DetailClient.Text = DetailSender.Text = DetailRecipient.Text = "";
        DetailClientPhone.Text = DetailRecipientPhone.Text = DetailRecipientAddress.Text = "";
        DetailZone.Text = DetailWeight.Text = DetailPrice.Text = DetailPayment.Text = "";
        DetailCourier.Text = DetailComment.Text = "";
        HistoryList.ItemsSource = null;
    }

    private int? SelectedId => _selected is null ? null : Convert.ToInt32(_selected["id_zakaza"]);

    private void Search_Click(object sender, RoutedEventArgs e) => LoadOrders();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoadOrders();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        StatusFilter.SelectedIndex = -1;
        DateFrom.SelectedDate = DateTime.Today.AddDays(-30);
        DateTo.SelectedDate = DateTime.Today;
        LoadOrders();
    }

    private void New_Click(object sender, RoutedEventArgs e) => CreateNewOrder();

    public void CreateNewOrder()
    {
        if (!AppSession.User.CanEditOrders) return;
        var dialog = new OrderDialog(null) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            LoadOrders();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e) => EditOrder();

    private void EditOrder()
    {
        if (SelectedId is not int id) return;
        var dialog = new OrderDialog(id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadOrders();
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        var zoneId = OrderService.GetOrderDto(id)?.ZoneId ?? 0;
        var dialog = new AssignCourierDialog(id, zoneId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadOrders();
    }

    private void Status_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id || _selected is null) return;
        var statusId = Convert.ToInt32(_selected["id_statusa"]);
        var dialog = new StatusDialog(id, statusId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadOrders();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        if (!Ui.Confirm("Отменить выбранный заказ? Действие можно будет увидеть в истории статусов.")) return;

        try
        {
            OrderService.Cancel(id, AppSession.User.UserId);
            LoadOrders();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Отмена заказа");
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, "Журнал заказов курьерской службы");
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.ExportCsv(_current, "Заказы");
    }
}
