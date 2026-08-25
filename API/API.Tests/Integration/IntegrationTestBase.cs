using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Data;
using API.Projects;
using API.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Integration;

// Test-only shapes for parsing response bodies. The real production DTOs
// (UserDto, ProjectDto, TicketDto) only expose a constructor taking the
// corresponding entity (e.g. ProjectDto(Project project)) - fine for the
// server, which only ever serializes them out, but System.Text.Json can't use
// that constructor to deserialize JSON back into an instance (the parameter
// name doesn't match any property name on the DTO). These simple positional
// records sidestep that entirely without touching production code, reading
// the same real JSON responses the server actually sends.
internal record TestUserDto(Guid UserId, string Username);
internal record TestAuthResponse(TestUserDto User, string Token);
internal record TestProjectDto(
    Guid ProjectId, string Title, string? Description, DateTime? Deadline,
    ProjectStatus Status, List<Guid> ParticipantIds, int TicketCount);
internal record TestTicketDto(
    Guid TicketId, string Title, string? Description, DateTime? Deadline,
    TicketStatus Status, Priority Priority, Guid ProjectId, Guid? AssigneeId,
    Guid AuthorId, List<string> Tags, int CommentCount);

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<(string Token, Guid UserId)> SignUp(string username, string password = "Sup3rSecret!")
    {
        var resp = await Client.PostAsJsonAsync("/api/Auth/signup", new { username, password });
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<TestAuthResponse>(JsonOptions);
        return (auth!.Token, auth.User.UserId);
    }

    protected void AuthenticateAs(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuth()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}