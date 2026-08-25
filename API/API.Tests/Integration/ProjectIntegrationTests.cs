using System.Net;
using System.Net.Http.Json;

namespace API.Tests.Integration;

public class ProjectIntegrationTests : IntegrationTestBase
{
    public ProjectIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(string token, Guid userId)> SignUpAndAuthenticate(string username)
    {
        var (token, userId) = await SignUp(username);
        AuthenticateAs(token);
        return (token, userId);
    }

    [Fact]
    public async Task CreateProject_ThenGetById_ReturnsSameProject()
    {
        await SignUpAndAuthenticate("owner1");

        var createResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "Test Project", description = "desc", deadline = (DateTime?)null });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        var getResp = await Client.GetAsync($"/api/Project?projectId={created!.ProjectId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        Assert.Equal("Test Project", fetched!.Title);
        Assert.Equal(created.ProjectId, fetched.ProjectId);
    }

    [Fact]
    public async Task CreateProject_CreatorIsAutomaticallyAParticipant()
    {
        var (_, userId) = await SignUpAndAuthenticate("owner2");

        var createResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "P", description = (string?)null, deadline = (DateTime?)null });
        var created = await createResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        Assert.Contains(userId, created!.ParticipantIds);
    }

    [Fact]
    public async Task CreateProject_EmptyTitle_ReturnsBadRequest()
    {
        await SignUpAndAuthenticate("owner3");

        var resp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "", description = (string?)null, deadline = (DateTime?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_AsNonParticipant_ReturnsForbidden()
    {
        await SignUpAndAuthenticate("owner4");
        var createResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "P", description = (string?)null, deadline = (DateTime?)null });
        var created = await createResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        var (outsiderToken, _) = await SignUp("outsider1");
        AuthenticateAs(outsiderToken);

        var updateResp = await Client.PutAsJsonAsync($"/api/Project?projectId={created!.ProjectId}",
            new { title = "Hijacked", description = (string?)null, deadline = (DateTime?)null });

        Assert.Equal(HttpStatusCode.Forbidden, updateResp.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_CascadesToItsTickets()
    {
        await SignUpAndAuthenticate("owner5");
        var projectResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "P", description = (string?)null, deadline = (DateTime?)null });
        var project = await projectResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        var ticketResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "MEDIUM", projectId = project!.ProjectId });
        Assert.Equal(HttpStatusCode.Created, ticketResp.StatusCode);
        var ticket = await ticketResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        var deleteResp = await Client.DeleteAsync($"/api/Project?projectId={project.ProjectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getTicketResp = await Client.GetAsync($"/api/Ticket?ticketId={ticket!.TicketId}");
        Assert.Equal(HttpStatusCode.NotFound, getTicketResp.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_InvalidGuidFormat_ReturnsBadRequest()
    {
        await SignUpAndAuthenticate("owner6");

        var resp = await Client.GetAsync("/api/Project?projectId=not-a-real-guid");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task RemoveParticipant_WhoHasAssignedTickets_TicketKeepsStaleAssignment_KnownLimitation()
    {
        // Documents a known, deliberately-unaddressed gap rather than desired
        // behavior: removing a participant does NOT touch tickets already
        // assigned to them, leaving the ticket pointing at someone no longer
        // on the project.
        await SignUpAndAuthenticate("owner7");
        var projectResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "P", description = (string?)null, deadline = (DateTime?)null });
        var project = await projectResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        var (_, memberId) = await SignUp("member1");
        await Client.PostAsync($"/api/Project/participants?projectId={project!.ProjectId}&userId={memberId}", null);

        var ticketResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project.ProjectId });
        var ticket = await ticketResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        var assignResp = await Client.PutAsync($"/api/Ticket/assign?ticketId={ticket!.TicketId}&assigneeId={memberId}", null);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        await Client.DeleteAsync($"/api/Project/participants?projectId={project.ProjectId}&userId={memberId}");

        var getTicketResp = await Client.GetAsync($"/api/Ticket?ticketId={ticket.TicketId}");
        var stillAssignedTicket = await getTicketResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        Assert.Equal(memberId, stillAssignedTicket!.AssigneeId);
    }
}