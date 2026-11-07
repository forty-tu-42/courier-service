using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Courier.Data;
using Courier.Shared;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class OrderDialog : Window
{
    private readonly int? _orderId;
    private OrderDto? _order;
    private bool _loading;

    public OrderDialog(int? orderId)
    {
        InitializeComponent();
        _orderId = orderId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _loading = true;
            var clients = ReferenceService.GetClients();
            if (_orderId is null)
            {
                clients.DefaultView.RowFilter = "is_active = true";
            }
            ClientBox.ItemsSource = clients.DefaultView;
            ZoneBox.ItemsSource = ReferenceService.GetZones().DefaultView;
            CourierBox.ItemsSource = ReferenceService.GetCouriers().DefaultView;

            if (_orderId is int id)
            {
                TitleText.Text = "Редактирование заказа";
                _order = OrderService.GetOrderDto(id);
                if (_order is null)
                {
                    Ui.Error(new InvalidOperationException("Заказ не найден."));
                    Close();
                    return;
                }

                ClientBox.SelectedValue = _order.ClientId;
                SenderBox.Text = _order.SenderAddress;
                RecipientFioBox.Text = _order.RecipientFio;
                RecipientPhoneBox.Text = _order.RecipientPhone;
                RecipientAddressBox.Text = _order.RecipientAddress;
                WeightBox.Text = _order.Weight.ToString("0.##", CultureInfo.CurrentCulture);
                ZoneBox.SelectedValue = _order.ZoneId;
                CourierBox.SelectedValue = _order.CourierId;
                CommentBox.Text = _order.Comment ?? "";
                SelectPayment(_order.PaymentMethod);
                CalculatePrice();
            }
            else
            {
                TitleText.Text = "Новый заказ";
                PaymentBox.SelectedIndex = 0;
                if (ClientBox.Items.Count > 0) ClientBox.SelectedIndex = 0;
                if (ZoneBox.Items.Count > 0) ZoneBox.SelectedIndex = 0;
                CalculatePrice();
            }
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Подготовка формы заказа");
        }
        finally
        {
            _loading = false;
        }
    }

    private void SelectPayment(string? method)
    {
        foreach (var item in PaymentBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), method, StringComparison.OrdinalIgnoreCase))
            {
                PaymentBox.SelectedItem = item;
                return;
            }
        }
        PaymentBox.SelectedIndex = 0;
    }

    private string? PaymentMethod =>
        (PaymentBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

    private void WeightOrZone_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        CalculatePrice();
    }

    private void CalculatePrice()
    {
        try
        {
            if (ZoneBox.SelectedValue is not int zoneId ||
                !decimal.TryParse(WeightBox.Text.Replace(',', '.'), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var weight) ||
                weight <= 0)
            {
                PriceText.Text = "—";
                return;
            }

            var tariff = OrderService.GetTariff(zoneId, weight);
            PriceText.Text = tariff.Price.ToString("N2") + " ₽";
        }
        catch
        {
            PriceText.Text = "нет тарифа";
        }
    }

    private void NewClient_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ClientDialog(null) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var clients = ReferenceService.GetClients();
            ClientBox.ItemsSource = clients.DefaultView;
            if (dialog.SavedId is int id)
                ClientBox.SelectedValue = id;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Новый клиент");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (ClientBox.SelectedValue is not int clientId)
        {
            ShowError("Выберите клиента.");
            return;
        }
        if (string.IsNullOrWhiteSpace(SenderBox.Text))
        {
            ShowError("Укажите адрес отправителя.");
            return;
        }
        if (string.IsNullOrWhiteSpace(RecipientFioBox.Text))
        {
            ShowError("Укажите ФИО получателя.");
            return;
        }
        if (string.IsNullOrWhiteSpace(RecipientPhoneBox.Text) || RecipientPhoneBox.Text.Length < 10)
        {
            ShowError("Укажите корректный телефон получателя (не менее 10 цифр).");
            return;
        }
        if (string.IsNullOrWhiteSpace(RecipientAddressBox.Text))
        {
            ShowError("Укажите адрес доставки.");
            return;
        }
        if (!decimal.TryParse(WeightBox.Text.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
        {
            ShowError("Укажите корректный вес заказа (больше нуля).");
            return;
        }
        if (ZoneBox.SelectedValue is not int zoneId)
        {
            ShowError("Выберите зону обслуживания.");
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;

            if (_orderId is int id)
            {
                var update = new UpdateOrderRequest
                {
                    ClientId = clientId,
                    SenderAddress = SenderBox.Text.Trim(),
                    RecipientFio = RecipientFioBox.Text.Trim(),
                    RecipientPhone = RecipientPhoneBox.Text.Trim(),
                    RecipientAddress = RecipientAddressBox.Text.Trim(),
                    Weight = weight,
                    ZoneId = zoneId,
                    Comment = CommentBox.Text.Trim(),
                    PaymentMethod = PaymentMethod,
                    CourierId = CourierBox.SelectedValue as int?
                };
                OrderService.Update(id, update, AppSession.User.UserId);
                DialogResult = true;
            }
            else
            {
                var create = new CreateOrderRequest
                {
                    ClientId = clientId,
                    SenderAddress = SenderBox.Text.Trim(),
                    RecipientFio = RecipientFioBox.Text.Trim(),
                    RecipientPhone = RecipientPhoneBox.Text.Trim(),
                    RecipientAddress = RecipientAddressBox.Text.Trim(),
                    Weight = weight,
                    ZoneId = zoneId,
                    Comment = CommentBox.Text.Trim(),
                    PaymentMethod = PaymentMethod,
                    CourierId = CourierBox.SelectedValue as int?
                };
                var result = OrderService.Create(create, AppSession.User.UserId);
                Ui.Info($"Заказ создан.\n\nНомер: {result.Nomer}\nСтоимость: {result.Price:N2} ₽",
                    "Новый заказ");
                DialogResult = true;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Сохранение заказа");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
