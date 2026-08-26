using System.Net;
using System.Net.Http.Json;

namespace API.Tests.Integration;

internal record TestCommentDto(Guid CommentId, string Text, Guid TicketId, Guid CommenterId, DateTime CreatedAt);

public class CommentIntegrationTests : IntegrationTestBase
{
    public CommentIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<(TestProjectDto project, TestTicketDto ticket)> CreateProjectAndTicket()
    {
        var projectResp = await Client.PostAsJsonAsync("/api/Project",
            new { title = "P", description = (string?)null, deadline = (DateTime?)null });
        var project = await projectResp.Content.ReadFromJsonAsync<TestProjectDto>(JsonOptions);

        var ticketResp = await Client.PostAsJsonAsync("/api/Ticket",
            new { title = "T", description = (string?)null, deadline = (DateTime?)null, priority = "LOW", projectId = project!.ProjectId });
        var ticket = await ticketResp.Content.ReadFromJsonAsync<TestTicketDto>(JsonOptions);

        return (project, ticket!);
    }

    [Fact]
    public async Task CreateComment_ThenGetByTicket_ReturnsComment()
    {
        var (token, commenterId) = await SignUp("commenter1");
        AuthenticateAs(token);
        var (_, ticket) = await CreateProjectAndTicket();

        var createResp = await Client.PostAsJsonAsync("/api/Comment", new { text = "Looks good!", ticketId = ticket.TicketId });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<TestCommentDto>(JsonOptions);
        Assert.Equal(commenterId, created!.CommenterId);

        var listResp = await Client.GetAsync($"/api/Comment/by-ticket?ticketId={ticket.TicketId}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var comments = await listResp.Content.ReadFromJsonAsync<List<TestCommentDto>>(JsonOptions);
        Assert.Single(comments!);
        Assert.Equal("Looks good!", comments![0].Text);
    }

    [Fact]
    public async Task CreateComment_EmptyText_ReturnsBadRequest()
    {
        var (token, _) = await SignUp("commenter2");
        AuthenticateAs(token);
        var (_, ticket) = await CreateProjectAndTicket();

        var resp = await Client.PostAsJsonAsync("/api/Comment", new { text = "", ticketId = ticket.TicketId });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateComment_AsNonProjectParticipant_ReturnsForbidden()
    {
        var (ownerToken, _) = await SignUp("commentowner1");
        AuthenticateAs(ownerToken);
        var (_, ticket) = await CreateProjectAndTicket();

        var (outsiderToken, _) = await SignUp("commentoutsider1");
        AuthenticateAs(outsiderToken);

        var resp = await Client.PostAsJsonAsync("/api/Comment", new { text = "Sneaky comment", ticketId = ticket.TicketId });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_AsAuthor_Succeeds()
    {
        var (token, _) = await SignUp("commenter3");
        AuthenticateAs(token);
        var (_, ticket) = await CreateProjectAndTicket();
        var createResp = await Client.PostAsJsonAsync("/api/Comment", new { text = "Delete me", ticketId = ticket.TicketId });
        var comment = await createResp.Content.ReadFromJsonAsync<TestCommentDto>(JsonOptions);

        var deleteResp = await Client.DeleteAsync($"/api/Comment?commentId={comment!.CommentId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_AsNonAuthor_ReturnsForbidden()
    {
        var (ownerToken, _) = await SignUp("commentowner2");
        AuthenticateAs(ownerToken);
        var (project, ticket) = await CreateProjectAndTicket();
        var createResp = await Client.PostAsJsonAsync("/api/Comment", new { text = "My comment", ticketId = ticket.TicketId });
        var comment = await createResp.Content.ReadFromJsonAsync<TestCommentDto>(JsonOptions);

        var (memberToken, memberId) = await SignUp("commentmember1");
        await Client.PostAsync($"/api/Project/participants?projectId={project.ProjectId}&userId={memberId}", null);
        AuthenticateAs(memberToken);

        var deleteResp = await Client.DeleteAsync($"/api/Comment?commentId={comment!.CommentId}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResp.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_CascadesToTicketCommentsToo()
    {
        var (token, _) = await SignUp("commenter4");
        AuthenticateAs(token);
        var (project, ticket) = await CreateProjectAndTicket();
        var createResp = await Client.PostAsJsonAsync("/api/Comment", new { text = "About to vanish", ticketId = ticket.TicketId });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var deleteResp = await Client.DeleteAsync($"/api/Project?projectId={project.ProjectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Ticket is gone (cascaded from project), so its comments query 404s too
        var listResp = await Client.GetAsync($"/api/Comment/by-ticket?ticketId={ticket.TicketId}");
        Assert.Equal(HttpStatusCode.NotFound, listResp.StatusCode);
    }
}