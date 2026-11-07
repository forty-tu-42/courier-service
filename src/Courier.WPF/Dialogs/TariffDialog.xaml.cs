using System.Globalization;
using System.Windows;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class TariffDialog : Window
{
    private readonly int? _tariffId;

    public TariffDialog(int? tariffId)
    {
        InitializeComponent();
        _tariffId = tariffId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            ZoneBox.ItemsSource = ReferenceService.GetZones().DefaultView;

            if (_tariffId is int id)
            {
                TitleText.Text = "Редактирование тарифа";
                var table = ReferenceService.GetTariff(id);
                if (table.Rows.Count == 0)
                {
                    Ui.Error(new InvalidOperationException("Тариф не найден."));
                    Close();
                    return;
                }

                var row = table.Rows[0];
                ZoneBox.SelectedValue = Convert.ToInt32(row["id_zony"]);
                FromBox.Text = Convert.ToDecimal(row["ves_ot"]).ToString("0.##", CultureInfo.CurrentCulture);
                ToBox.Text = Convert.ToDecimal(row["ves_do"]).ToString("0.##", CultureInfo.CurrentCulture);
                PriceBox.Text = Convert.ToDecimal(row["cena"]).ToString("0.##", CultureInfo.CurrentCulture);
            }
            else
            {
                if (ZoneBox.Items.Count > 0) ZoneBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Тариф");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (ZoneBox.SelectedValue is not int zoneId)
        {
            ShowError("Выберите зону обслуживания.");
            return;
        }
        if (!TryDecimal(FromBox.Text, out var from) || !TryDecimal(ToBox.Text, out var to) ||
            !TryDecimal(PriceBox.Text, out var price))
        {
            ShowError("Проверьте числовые значения веса и стоимости.");
            return;
        }
        if (to <= from)
        {
            ShowError("Верхняя граница веса должна быть больше нижней.");
            return;
        }
        if (price < 0)
        {
            ShowError("Стоимость не может быть отрицательной.");
            return;
        }

        try
        {
            ReferenceService.SaveTariff(_tariffId, zoneId, from, to, price);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Сохранение тарифа");
        }
    }

    private static bool TryDecimal(string text, out decimal value)
        => decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out value);

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
