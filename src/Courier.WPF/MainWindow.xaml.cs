using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Courier.Data;
using Courier.WPF.Helpers;
using Courier.WPF.Views;

namespace Courier.WPF;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, Button> _navButtons = new();

    public MainWindow()
    {
        InitializeComponent();
        _navButtons["dashboard"] = NavDashboard;
        _navButtons["orders"] = NavOrders;
        _navButtons["couriers"] = NavCouriers;
        _navButtons["clients"] = NavClients;
        _navButtons["tariffs"] = NavTariffs;
        _navButtons["reports"] = NavReports;
        _navButtons["users"] = NavUsers;
        _navButtons["audit"] = NavAudit;

        Loaded += MainWindow_Loaded;
        Closing += (_, _) => AuditService.Log(AppSession.User.Login, "Выход из системы");
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var user = AppSession.User;
        UserText.Text = user.Fio;
        RoleText.Text = user.Role;
        AvatarText.Text = GetInitials(user.Fio);

        ApplyRoleVisibility();
        Navigate(user.IsCourier ? "orders" : "dashboard");
    }

    private void ApplyRoleVisibility()
    {
        var user = AppSession.User;

        NavUsers.Visibility = user.CanManageUsers ? Visibility.Visible : Visibility.Collapsed;
        NavAudit.Visibility = user.CanViewAudit ? Visibility.Visible : Visibility.Collapsed;
        NavDashboard.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        NavCouriers.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        NavClients.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        NavTariffs.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        NavReports.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        SectionReferences.Visibility = user.IsCourier ? Visibility.Collapsed : Visibility.Visible;
        SectionAdmin.Visibility = user.CanManageUsers || user.CanViewAudit
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (user.IsCourier)
            NavOrders.Content = "Мои заказы";
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string page })
            Navigate(page);
    }

    public void Navigate(string page)
    {
        try
        {
            switch (page)
            {
                case "dashboard":
                    PageTitleText.Text = "Панель управления";
                    PageSubtitleText.Text = "сводка по работе курьерской службы";
                    PageHost.Content = new DashboardView();
                    break;
                case "orders":
                    PageTitleText.Text = AppSession.IsCourier ? "Мои заказы" : "Заказы";
                    PageSubtitleText.Text = "учёт, назначение курьеров и статусы";
                    PageHost.Content = new OrdersView(AppSession.IsCourier ? AppSession.CourierId : null);
                    break;
                case "couriers":
                    PageTitleText.Text = "Курьеры";
                    PageSubtitleText.Text = "сотрудники доставки, транспорт и зоны";
                    PageHost.Content = new CouriersView();
                    break;
                case "clients":
                    PageTitleText.Text = "Клиенты";
                    PageSubtitleText.Text = "база заказчиков курьерской службы";
                    PageHost.Content = new ClientsView();
                    break;
                case "tariffs":
                    PageTitleText.Text = "Тарифы и зоны";
                    PageSubtitleText.Text = "стоимость доставки по зонам и весу";
                    PageHost.Content = new TariffsView();
                    break;
                case "reports":
                    PageTitleText.Text = AppSession.IsCourier ? "Мои отчёты" : "Отчёты";
                    PageSubtitleText.Text = "доставки, статусы, загрузка курьеров и выручка";
                    PageHost.Content = new ReportsView(AppSession.IsCourier ? AppSession.CourierId : null);
                    break;
                case "users":
                    PageTitleText.Text = "Пользователи";
                    PageSubtitleText.Text = "учётные записи и роли";
                    PageHost.Content = new UsersView();
                    break;
                case "audit":
                    PageTitleText.Text = "Журнал аудита";
                    PageSubtitleText.Text = "действия пользователей в системе";
                    PageHost.Content = new AuditView();
                    break;
                case "about":
                    new AboutWindow { Owner = this }.ShowDialog();
                    return;
            }

            SetActiveNav(page);
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Ошибка открытия раздела");
        }
    }

    private void SetActiveNav(string page)
    {
        foreach (var (key, button) in _navButtons)
        {
            var active = key == page;
            button.Background = active
                ? new SolidColorBrush(Color.FromRgb(17, 94, 89))
                : Brushes.Transparent;
            button.Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225));
            button.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private static string GetInitials(string fio)
    {
        var parts = fio.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpper(),
            _ => (parts[0][..1] + parts[1][..1]).ToUpper()
        };
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (!Ui.Confirm("Выйти из системы?", "Выход")) return;
        Application.Current.Shutdown();
    }
}
