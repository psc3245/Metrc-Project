using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace API.Tests.Integration;

public class TicketIntegrationTests : IntegrationTestBase
{
    public TicketIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<TestProjectDto> CreateProjectAsCurrentUser(string title = "P")
    {
        var resp = await Client.PostAsJsonAsync("/api/Project", new { title, description = (string?)null, deadline = (DateTime?)null });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions))!;
    }

    [Fact]
    public async Task FullTicketLifecycle_CreateAssignUpdateTagRemove_Succeeds()
    {
        var (ownerToken, ownerId) = await SignUp("ticketowner1");
        AuthenticateAs(ownerToken);
        var project = await CreateProjectAsCurrentUser();

        var (_, memberId) = await SignUp("ticketmember1");
        await Client.PostAsync($"/api/Project/participants?projectId={project.ProjectId}&userId={memberId}", null);

        var createResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "Fix bug", description = "desc", deadline = (DateTime?)null, priority = "HIGH", projectId = project.ProjectId });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var ticket = await createResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);
        Assert.Equal(ownerId, ticket!.AuthorId);

        var assignResp = await Client.PutAsync($"/api/Ticket/assign?ticketId={ticket.TicketId}&assigneeId={memberId}", null);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);
        var assigned = await assignResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);
        Assert.Equal(memberId, assigned!.AssigneeId);

        var updateResp = await Client.PutAsJsonAsync($"/api/Ticket?ticketId={ticket.TicketId}",
            new { title = (string?)null, description = (string?)null, deadline = (DateTime?)null, status = "IN_PROGRESS", priority = (string?)null });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);
        Assert.Equal(API.Tickets.TicketStatus.IN_PROGRESS, updated!.Status);

        var tagResp = await Client.PostAsJsonAsync($"/api/Ticket/tags?ticketId={ticket.TicketId}", new { name = "bug", color = "#FF0000" });
        Assert.Equal(HttpStatusCode.OK, tagResp.StatusCode);
        var tagged = await tagResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);
        Assert.Contains("bug", tagged!.Tags);

        var removeTagResp = await Client.DeleteAsync($"/api/Ticket/tags?ticketId={ticket.TicketId}&tagName=bug");
        Assert.Equal(HttpStatusCode.OK, removeTagResp.StatusCode);
        var untagged = await removeTagResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);
        Assert.DoesNotContain("bug", untagged!.Tags);
    }

    [Fact]
    public async Task CreateTicket_EmptyTitle_ReturnsBadRequest()
    {
        var (token, _) = await SignUp("ticketowner2");
        AuthenticateAs(token);
        var project = await CreateProjectAsCurrentUser();

        var resp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_InvalidPriorityValue_ReturnsBadRequest()
    {
        var (token, _) = await SignUp("ticketowner3");
        AuthenticateAs(token);
        var project = await CreateProjectAsCurrentUser();

        var resp = await Client.PostAsync("/api/Ticket",
            new StringContent(
                $"{{\"title\":\"T\",\"description\":null,\"deadline\":null,\"priority\":\"NOT_A_REAL_PRIORITY\",\"projectId\":\"{project.ProjectId}\"}}",
                Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_InvalidStatusValue_ReturnsBadRequest()
    {
        var (token, _) = await SignUp("ticketowner4");
        AuthenticateAs(token);
        var project = await CreateProjectAsCurrentUser();
        var createResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });
        var ticket = await createResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        var resp = await Client.PutAsync($"/api/Ticket?ticketId={ticket!.TicketId}",
            new StringContent(
                "{\"title\":null,\"description\":null,\"deadline\":null,\"status\":\"NOT_A_REAL_STATUS\",\"priority\":null}",
                Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_ToNonProjectParticipant_ReturnsForbidden()
    {
        var (token, _) = await SignUp("ticketowner5");
        AuthenticateAs(token);
        var project = await CreateProjectAsCurrentUser();
        var createResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });
        var ticket = await createResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        var (_, outsiderId) = await SignUp("outsider2");

        var assignResp = await Client.PutAsync($"/api/Ticket/assign?ticketId={ticket!.TicketId}&assigneeId={outsiderId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, assignResp.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_AsNonProjectParticipant_ReturnsForbidden()
    {
        var (ownerToken, _) = await SignUp("ticketowner6");
        AuthenticateAs(ownerToken);
        var project = await CreateProjectAsCurrentUser();

        var (outsiderToken, _) = await SignUp("outsider3");
        AuthenticateAs(outsiderToken);

        var resp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task AddTag_Twice_DoesNotDuplicate()
    {
        var (token, _) = await SignUp("ticketowner7");
        AuthenticateAs(token);
        var project = await CreateProjectAsCurrentUser();
        var createResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });
        var ticket = await createResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        await Client.PostAsJsonAsync($"/api/Ticket/tags?ticketId={ticket!.TicketId}", new { name = "urgent", color = "#FF0000" });
        var secondResp = await Client.PostAsJsonAsync($"/api/Ticket/tags?ticketId={ticket.TicketId}", new { name = "urgent", color = "#00FF00" });
        var result = await secondResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        Assert.Single(result!.Tags);
    }
}