using System.Windows;
using System.Windows.Controls;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class CourierDialog : Window
{
    private readonly int? _courierId;

    public CourierDialog(int? courierId)
    {
        InitializeComponent();
        _courierId = courierId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            ZoneBox.ItemsSource = ReferenceService.GetZones().DefaultView;
            var users = ReferenceService.GetUsers();
            users.DefaultView.RowFilter = "rol = 'Курьер'";
            UserBox.ItemsSource = users.DefaultView;

            if (_courierId is int id)
            {
                TitleText.Text = "Редактирование курьера";
                var table = ReferenceService.GetCourier(id);
                if (table.Rows.Count == 0)
                {
                    Ui.Error(new InvalidOperationException("Курьер не найден."));
                    Close();
                    return;
                }

                var row = table.Rows[0];
                FioBox.Text = row["fio"]?.ToString() ?? "";
                PhoneBox.Text = row["telefon"]?.ToString() ?? "";
                SelectCombo(TransportBox, row["transport"]?.ToString());
                ZoneBox.SelectedValue = Convert.ToInt32(row["id_zony"]);
                SelectCombo(StatusBox, row["status"]?.ToString());
                ActiveBox.IsChecked = row["is_active"] is bool b && b;
                UserBox.SelectedValue = row["id_polzovatelya"] is DBNull
                    ? null
                    : Convert.ToInt32(row["id_polzovatelya"]);
            }
            else
            {
                TransportBox.SelectedIndex = 0;
                StatusBox.SelectedIndex = 0;
                if (ZoneBox.Items.Count > 0) ZoneBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Курьер");
        }
    }

    private static void SelectCombo(ComboBox combo, string? value)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private static string ComboText(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(FioBox.Text))
        {
            ShowError("Укажите ФИО курьера.");
            return;
        }
        if (string.IsNullOrWhiteSpace(PhoneBox.Text) || PhoneBox.Text.Length < 10)
        {
            ShowError("Укажите корректный телефон (не менее 10 цифр).");
            return;
        }
        if (ZoneBox.SelectedValue is not int zoneId)
        {
            ShowError("Выберите зону обслуживания.");
            return;
        }

        try
        {
            ReferenceService.SaveCourier(
                _courierId,
                UserBox.SelectedValue as int?,
                FioBox.Text.Trim(),
                PhoneBox.Text.Trim(),
                ComboText(TransportBox),
                zoneId,
                ComboText(StatusBox),
                ActiveBox.IsChecked == true);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Сохранение курьера");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
