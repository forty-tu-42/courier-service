using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Courier.Data;
using Courier.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Courier.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_MatchesSqlSeedFormula()
    {
        // Значение получено из SQL: CONVERT(CHAR(64), HASHBYTES('SHA2_256', N'соль'+N'пароль'), 2)
        var hash = PasswordHasher.Hash("admin123", "a1b2c3d4e5f60718");
        Assert.Equal("F3AD40E68EF7779BAB454EAD1A31572CDADB47BA3846BB3D9169C55798CC69B7", hash);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var salt = "b2c3d4e5f6071829";
        var hash = PasswordHasher.Hash("manager123", salt);
        Assert.True(PasswordHasher.Verify("manager123", salt, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var salt = "c3d4e5f60718293a";
        var hash = PasswordHasher.Hash("courier123", salt);
        Assert.False(PasswordHasher.Verify("wrong", salt, hash));
    }
}

public class TariffTests
{
    [Fact]
    public void GetTariff_ReturnsPositivePrice_ForKnownZoneAndWeight()
    {
        var tariff = OrderService.GetTariff(1, 0.5m);
        Assert.True(tariff.Price > 0);
        Assert.True(tariff.TariffId > 0);
    }

    [Fact]
    public void GetTariff_Throws_ForTooHeavyOrder()
    {
        Assert.Throws<InvalidOperationException>(() => OrderService.GetTariff(1, 9999m));
    }
}

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Login = "admin",
            Password = "admin123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.Token));
        Assert.Equal("Admin", login.User.RoleCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Login = "admin",
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithAdminToken_ReturnsList()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Login = "admin",
            Password = "admin123"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);

        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.NotEmpty(orders!);
    }

    [Fact]
    public async Task Courier_CannotReadAllClients()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Login = "courier",
            Password = "courier123"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);

        var response = await client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
