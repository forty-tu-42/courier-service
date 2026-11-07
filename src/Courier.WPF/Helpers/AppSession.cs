using Courier.Shared;

namespace Courier.WPF.Helpers;

public static class AppSession
{
    public static UserSession? CurrentUser { get; set; }

    public static UserSession User =>
        CurrentUser ?? throw new InvalidOperationException("Пользователь не авторизован.");

    public static bool IsAdmin => CurrentUser?.IsAdmin == true;
    public static bool IsManager => CurrentUser?.IsManager == true;
    public static bool IsCourier => CurrentUser?.IsCourier == true;
    public static int? CourierId => CurrentUser?.CourierId;
}
