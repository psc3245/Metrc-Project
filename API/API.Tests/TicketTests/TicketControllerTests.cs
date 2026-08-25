using System.Security.Claims;
using API.Controllers;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Service;
using API.Tickets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.TicketTests;

public class TicketControllerTests
{
    private readonly Mock<ITicketService> _serviceMock;
    private readonly TicketController _controller;
    private readonly Guid _authenticatedUserId = Guid.NewGuid();

    public TicketControllerTests()
    {
        _serviceMock = new Mock<ITicketService>();
        _controller = new TicketController(_serviceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _authenticatedUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static TicketDto MakeDto(Guid id, string title, Guid authorId) =>
        new TicketDto(new Ticket { Id = id, Title = title, ProjectId = Guid.NewGuid(), AuthorId = authorId });

    [Fact]
    public async Task CreateTicket_UsesAuthenticatedUserIdAsAuthor_ReturnsCreatedAtAction()
    {
        var ticketId = Guid.NewGuid();
        var dto = MakeDto(ticketId, "New Ticket", _authenticatedUserId);
        _serviceMock.Setup(s => s.CreateTicket(It.IsAny<CreateTicketRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, Guid.NewGuid());
        var result = await _controller.CreateTicket(req);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedDto = Assert.IsType<TicketDto>(createdResult.Value);
        Assert.Equal(_authenticatedUserId, returnedDto.AuthorId);
        _serviceMock.Verify(s => s.CreateTicket(req, _authenticatedUserId), Times.Once);
    }

    [Fact]
    public async Task CreateTicket_WhenCallerNotProjectParticipant_ReturnsForbidden()
    {
        _serviceMock.Setup(s => s.CreateTicket(It.IsAny<CreateTicketRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, Guid.NewGuid());
        var result = await _controller.CreateTicket(req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_WithNonexistentProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CreateTicket(It.IsAny<CreateTicketRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        var req = new CreateTicketRequest("New Ticket", null, null, Priority.MEDIUM, projectId);
        var result = await _controller.CreateTicket(req);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetTicketById_WithExistingTicket_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Existing", Guid.NewGuid());
        _serviceMock.Setup(s => s.GetTicketById(id)).ReturnsAsync(dto);

        var result = await _controller.GetTicketById(id);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTicketById_WithNonexistentTicket_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetTicketById(id)).ThrowsAsync(new TicketNotFoundException(id));

        var result = await _controller.GetTicketById(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAllTickets_ReturnsOkWithList()
    {
        var dtos = new List<TicketDto>
        {
            MakeDto(Guid.NewGuid(), "T1", Guid.NewGuid()),
            MakeDto(Guid.NewGuid(), "T2", Guid.NewGuid())
        };
        _serviceMock.Setup(s => s.GetAllTickets()).ReturnsAsync(dtos);

        var result = await _controller.GetAllTickets();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetTicketsByProjectId_WithNonexistentProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetTicketsByProjectId(projectId))
            .ThrowsAsync(new ProjectNotFoundException(projectId));

        var result = await _controller.GetTicketsByProjectId(projectId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateTicket_WithExistingTicket_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = MakeDto(id, "Updated", Guid.NewGuid());
        _serviceMock.Setup(s => s.UpdateTicket(id, It.IsAny<UpdateTicketRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var req = new UpdateTicketRequest("Updated", null, null, null, null);
        var result = await _controller.UpdateTicket(id, req);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateTicket_WhenCallerNotParticipant_ReturnsForbidden()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UpdateTicket(id, It.IsAny<UpdateTicketRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var req = new UpdateTicketRequest("Updated", null, null, null, null);
        var result = await _controller.UpdateTicket(id, req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteTicket_WithExistingTicket_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveTicket(id, _authenticatedUserId)).ReturnsAsync(true);

        var result = await _controller.DeleteTicket(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteTicket_WithNonexistentTicket_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveTicket(id, _authenticatedUserId)).ThrowsAsync(new TicketNotFoundException(id));

        var result = await _controller.DeleteTicket(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AssignTicket_Success_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var dto = MakeDto(ticketId, "T", Guid.NewGuid());
        _serviceMock.Setup(s => s.AssignTicket(ticketId, assigneeId, _authenticatedUserId)).ReturnsAsync(dto);

        var result = await _controller.AssignTicket(ticketId, assigneeId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignTicket_ToNonParticipant_ReturnsForbidden()
    {
        var ticketId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AssignTicket(ticketId, assigneeId, _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("Assignee must be a participant of the ticket's project."));

        var result = await _controller.AssignTicket(ticketId, assigneeId);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_UserNotFound_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _serviceMock.Setup(s => s.AssignTicket(ticketId, assigneeId, _authenticatedUserId))
            .ThrowsAsync(new UserNotFoundException(assigneeId));

        var result = await _controller.AssignTicket(ticketId, assigneeId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UnassignTicket_Success_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        var dto = MakeDto(ticketId, "T", Guid.NewGuid());
        _serviceMock.Setup(s => s.UnassignTicket(ticketId, _authenticatedUserId)).ReturnsAsync(dto);

        var result = await _controller.UnassignTicket(ticketId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddTag_Success_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        var dto = MakeDto(ticketId, "T", Guid.NewGuid());
        _serviceMock.Setup(s => s.AddTag(ticketId, It.IsAny<AddTagRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var result = await _controller.AddTag(ticketId, new AddTagRequest("bug", null));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveTag_Success_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        var dto = MakeDto(ticketId, "T", Guid.NewGuid());
        _serviceMock.Setup(s => s.RemoveTag(ticketId, "bug", _authenticatedUserId)).ReturnsAsync(dto);

        var result = await _controller.RemoveTag(ticketId, "bug");

        Assert.IsType<OkObjectResult>(result);
    }
}