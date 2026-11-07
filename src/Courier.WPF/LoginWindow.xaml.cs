using System.Windows;
using Courier.Data;
using Courier.Shared;
using Courier.WPF.Helpers;

namespace Courier.WPF;

public partial class LoginWindow : Window
{
    public UserSession? Session { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoginBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        try
        {
            LoginButton.IsEnabled = false;
            var session = AuthService.Login(login, password);
            if (session is null)
            {
                ShowError("Неверный логин или пароль, либо учётная запись заблокирована.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            Session = session;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError("Не удалось подключиться к базе данных.\n" +
                      "Проверьте, что скрипт KURS_KS_full.sql выполнен в SSMS.\n\n" +
                      ex.Message);
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
