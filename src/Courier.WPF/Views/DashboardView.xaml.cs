using System.Windows;
using System.Windows.Controls;
using Courier.Data;
using Courier.WPF.Helpers;

namespace Courier.WPF.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            var stats = OrderService.GetDashboard();
            TodayText.Text = stats.OrdersToday.ToString();
            InWorkText.Text = stats.OrdersInWork.ToString();
            DeliveredText.Text = stats.DeliveredThisMonth.ToString();
            FreeCouriersText.Text = stats.FreeCouriers.ToString();
            RevenueText.Text = stats.RevenueThisMonth.ToString("N0") + " ₽";

            RecentGrid.ItemsSource = OrderService.GetOrders().DefaultView;
            CouriersGrid.ItemsSource = ReferenceService.GetCouriers().DefaultView;
        }
        catch (Exception ex)
        {
            Ui.Error(ex, "Панель управления");
        }
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
        => OpenOrders("new");

    private void NewClient_Click(object sender, RoutedEventArgs e)
        => ((MainWindow)Window.GetWindow(this)!).Navigate("clients");

    private void Reports_Click(object sender, RoutedEventArgs e)
        => ((MainWindow)Window.GetWindow(this)!).Navigate("reports");

    private void AllOrders_Click(object sender, RoutedEventArgs e)
        => ((MainWindow)Window.GetWindow(this)!).Navigate("orders");

    private void AllCouriers_Click(object sender, RoutedEventArgs e)
        => ((MainWindow)Window.GetWindow(this)!).Navigate("couriers");

    private void OpenOrders(string action)
    {
        var main = (MainWindow)Window.GetWindow(this)!;
        main.Navigate("orders");
        if (main.FindName("PageHost") is ContentControl { Content: OrdersView orders })
            orders.CreateNewOrder();
    }
}
