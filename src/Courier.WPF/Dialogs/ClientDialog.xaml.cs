using System.Windows;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class ClientDialog : Window
{
    private readonly int? _clientId;
    public int? SavedId { get; private set; }

    public ClientDialog(int? clientId)
    {
        InitializeComponent();
        _clientId = clientId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        if (_clientId is not int id) return;

        try
        {
            TitleText.Text = "Редактирование клиента";
            var table = ReferenceService.GetClient(id);
            if (table.Rows.Count == 0)
            {
                Ui.Error(new InvalidOperationException("Клиент не найден."));
                Close();
                return;
            }

            var row = table.Rows[0];
            FioBox.Text = row["fio"]?.ToString() ?? "";
            PhoneBox.Text = row["telefon"]?.ToString() ?? "";
            EmailBox.Text = row["email"] is DBNull ? "" : row["email"]?.ToString();
            AddressBox.Text = row["adres"] is DBNull ? "" : row["adres"]?.ToString();
            ActiveBox.IsChecked = row["is_active"] is bool b ? b : true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Клиент");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(FioBox.Text))
        {
            ShowError("Укажите ФИО клиента.");
            return;
        }
        if (string.IsNullOrWhiteSpace(PhoneBox.Text) || PhoneBox.Text.Length < 10)
        {
            ShowError("Укажите корректный телефон (не менее 10 цифр).");
            return;
        }

        try
        {
            ReferenceService.SaveClient(
                _clientId,
                FioBox.Text.Trim(),
                PhoneBox.Text.Trim(),
                string.IsNullOrWhiteSpace(EmailBox.Text) ? null : EmailBox.Text.Trim(),
                string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
                ActiveBox.IsChecked == true);

            SavedId = _clientId ?? ReferenceService.GetClientIdByPhone(PhoneBox.Text.Trim());
            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Сохранение клиента");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
