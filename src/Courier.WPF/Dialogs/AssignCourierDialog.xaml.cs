using System.Data;
using System.Windows;
using System.Windows.Controls;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class AssignCourierDialog : Window
{
    private readonly int _orderId;
    private readonly int _zoneId;
    private DataRowView? _selected;

    public AssignCourierDialog(int orderId, int zoneId)
    {
        InitializeComponent();
        _orderId = orderId;
        _zoneId = zoneId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            var order = OrderService.GetOrderDto(_orderId);
            OrderText.Text = order is null
                ? $"Заказ №{_orderId}"
                : $"Заказ {order.Nomer} · {order.Zone} · {order.Weight:N2} кг · {order.Price:N2} ₽";

            var table = ReferenceService.GetFreeCouriers(_zoneId);
            if (table.Rows.Count == 0)
            {
                HintText.Text = "В этой зоне нет свободных курьеров. Показаны все свободные курьеры.";
                table = ReferenceService.GetFreeCouriers();
            }
            else
            {
                HintText.Text = "Выберите свободного курьера. После назначения заказ получит статус «Назначен».";
            }

            CourierGrid.ItemsSource = table.DefaultView;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Назначение курьера");
        }
    }

    private void CourierGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = CourierGrid.SelectedItem as DataRowView;
        AssignButton.IsEnabled = _selected is not null;
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;

        try
        {
            var courierId = Convert.ToInt32(_selected["id_kuriera"]);
            OrderService.AssignCourier(_orderId, courierId, AppSession.User.UserId);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Назначение курьера");
        }
    }
}
