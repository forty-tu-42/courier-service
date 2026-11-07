using System.Windows;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class StatusDialog : Window
{
    private readonly int _orderId;
    private readonly int _currentStatusId;

    public StatusDialog(int orderId, int currentStatusId)
    {
        InitializeComponent();
        _orderId = orderId;
        _currentStatusId = currentStatusId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            var order = OrderService.GetOrderDto(_orderId);
            OrderText.Text = order is null ? $"Заказ №{_orderId}" : $"{order.Nomer} · текущий статус: {order.Status}";
            StatusBox.ItemsSource = ReferenceService.GetStatuses().DefaultView;
            StatusBox.SelectedValue = _currentStatusId;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Статус заказа");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (StatusBox.SelectedValue is not int statusId)
        {
            Ui.Warn("Выберите статус.");
            return;
        }

        try
        {
            OrderService.ChangeStatus(_orderId, statusId, AppSession.User.UserId,
                string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text.Trim());
            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Изменение статуса");
        }
    }
}
