using System.Windows;
using Microsoft.Data.SqlClient;

namespace Courier.WPF.Helpers;

public static class Ui
{
    public static void Error(Exception ex, string? title = null)
    {
        var message = ex switch
        {
            SqlException sql when sql.Number is 2627 or 2601 =>
                "Запись с такими данными уже существует. Проверьте уникальные поля (логин, телефон, номер заказа).",
            SqlException sql when sql.Number == 547 =>
                "Запись используется в других таблицах и не может быть удалена.",
            SqlException sql when sql.Number is 50001 or 50002 or 50003 or 50004
                or 50010 or 50011 or 50012 or 50013 or 50014 or 50020 or 50021 =>
                sql.Message,
            InvalidOperationException => ex.Message,
            _ => ex.Message
        };

        MessageBox.Show(message, title ?? "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void Info(string message, string title = "Информация")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static bool Confirm(string message, string title = "Подтверждение")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public static void Warn(string message, string title = "Внимание")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
}
