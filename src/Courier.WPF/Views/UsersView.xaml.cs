using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Courier.Data;
using Courier.WPF.Dialogs;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class UsersView : UserControl
{
    private DataTable? _current;

    public UsersView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _current = ReferenceService.GetUsers(SearchBox.Text);
            UsersGrid.ItemsSource = _current.DefaultView;
            CountText.Text = $"Пользователей: {_current.Rows.Count}";
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Пользователи");
        }
    }

    private int? SelectedId
        => UsersGrid.SelectedItem is DataRowView row ? Convert.ToInt32(row["id_polzovatelya"]) : null;

    private void Search_Click(object sender, RoutedEventArgs e) => LoadData();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoadData();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        LoadData();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new UserDialog(null) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        var dialog = new UserDialog(id) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) LoadData();
    }

    private void UsersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => Edit_Click(sender, e);

    private void Block_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedId is not int id) return;
        if (!Ui.Confirm("Заблокировать выбранного пользователя?")) return;

        try
        {
            ReferenceService.ArchiveUser(id, AppSession.User.UserId);
            LoadData();
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Блокировка пользователя");
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        PrintHelper.PrintTable(_current, "Пользователи системы");
    }
}
