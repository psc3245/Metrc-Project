using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace API.Tests.Integration;

public class AuthIntegrationTests : IntegrationTestBase
{
    public AuthIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SignUp_ThenLogin_Succeeds()
    {
        var signupResp = await Client.PostAsJsonAsync("/api/Auth/signup", new { username = "alice", password = "Sup3rSecret!" });
        Assert.Equal(HttpStatusCode.OK, signupResp.StatusCode);

        var loginResp = await Client.PostAsJsonAsync("/api/Auth/login", new { username = "alice", password = "Sup3rSecret!" });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
    }

    [Fact]
    public async Task SignUp_DuplicateUsername_ReturnsConflict()
    {
        await Client.PostAsJsonAsync("/api/Auth/signup", new { username = "bob", password = "Sup3rSecret!" });
        var resp = await Client.PostAsJsonAsync("/api/Auth/signup", new { username = "bob", password = "Sup3rSecret!" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Client.PostAsJsonAsync("/api/Auth/signup", new { username = "carol", password = "Sup3rSecret!" });
        var resp = await Client.PostAsJsonAsync("/api/Auth/login", new { username = "carol", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNoToken_ReturnsUnauthorized()
    {
        var resp = await Client.GetAsync("/api/User/all");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this-is-not-a-real-jwt");

        var resp = await Client.GetAsync("/api/User/all");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        using var scope = Factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var jwtSection = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiredToken = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        var resp = await Client.GetAsync("/api/User/all");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}