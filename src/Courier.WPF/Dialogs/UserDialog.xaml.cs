using System.Windows;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Dialogs;

public partial class UserDialog : Window
{
    private readonly int? _userId;

    public UserDialog(int? userId)
    {
        InitializeComponent();
        _userId = userId;
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            RoleBox.ItemsSource = ReferenceService.GetRoles().DefaultView;

            if (_userId is int id)
            {
                TitleText.Text = "Редактирование пользователя";
                PasswordLabel.Text = "Новый пароль";
                PasswordHint.Text = "Оставьте пустым, чтобы не менять пароль";

                var table = ReferenceService.GetUser(id);
                if (table.Rows.Count == 0)
                {
                    Ui.Error(new InvalidOperationException("Пользователь не найден."));
                    Close();
                    return;
                }

                var row = table.Rows[0];
                LoginBox.Text = row["login"]?.ToString() ?? "";
                FioBox.Text = row["fio"]?.ToString() ?? "";
                PhoneBox.Text = row["telefon"] is DBNull ? "" : row["telefon"]?.ToString();
                RoleBox.SelectedValue = Convert.ToInt32(row["id_roli"]);
                ActiveBox.IsChecked = row["is_active"] is bool b && b;
            }
            else
            {
                if (RoleBox.Items.Count > 0) RoleBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Пользователь");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(LoginBox.Text) || LoginBox.Text.Trim().Length < 3)
        {
            ShowError("Логин должен быть не короче 3 символов.");
            return;
        }
        if (_userId is null && (string.IsNullOrWhiteSpace(PasswordBox.Password) || PasswordBox.Password.Length < 6))
        {
            ShowError("Пароль должен быть не короче 6 символов.");
            return;
        }
        if (_userId is not null && PasswordBox.Password.Length is > 0 and < 6)
        {
            ShowError("Новый пароль должен быть не короче 6 символов.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FioBox.Text))
        {
            ShowError("Укажите ФИО пользователя.");
            return;
        }
        if (RoleBox.SelectedValue is not int roleId)
        {
            ShowError("Выберите роль.");
            return;
        }

        try
        {
            ReferenceService.SaveUser(
                _userId,
                LoginBox.Text.Trim(),
                PasswordBox.Password,
                roleId,
                FioBox.Text.Trim(),
                string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
                ActiveBox.IsChecked == true);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Сохранение пользователя");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
