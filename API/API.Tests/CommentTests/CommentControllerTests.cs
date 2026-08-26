using System.Security.Claims;
using API.Comments;
using API.Controllers;
using API.Entities.Dtos;
using API.Entities.Exceptions;
using API.Entities.HelperClasses;
using API.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.CommentTests;

public class CommentControllerTests
{
    private readonly Mock<ICommentService> _serviceMock;
    private readonly CommentController _controller;
    private readonly Guid _authenticatedUserId = Guid.NewGuid();

    public CommentControllerTests()
    {
        _serviceMock = new Mock<ICommentService>();
        _controller = new CommentController(_serviceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _authenticatedUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static CommentDto MakeDto(Guid id, string text, Guid ticketId, Guid commenterId) =>
        new CommentDto(new Comment { Id = id, Text = text, TicketId = ticketId, CommenterId = commenterId });

    [Fact]
    public async Task CreateComment_UsesAuthenticatedUserIdAsCommenter_ReturnsCreatedAtAction()
    {
        var ticketId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var dto = MakeDto(commentId, "Nice work!", ticketId, _authenticatedUserId);
        _serviceMock.Setup(s => s.CreateComment(It.IsAny<CreateCommentRequest>(), _authenticatedUserId))
            .ReturnsAsync(dto);

        var req = new CreateCommentRequest("Nice work!", ticketId);
        var result = await _controller.CreateComment(req);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedDto = Assert.IsType<CommentDto>(createdResult.Value);
        Assert.Equal(_authenticatedUserId, returnedDto.CommenterId);
        _serviceMock.Verify(s => s.CreateComment(req, _authenticatedUserId), Times.Once);
    }

    [Fact]
    public async Task CreateComment_WhenCallerNotParticipant_ReturnsForbidden()
    {
        _serviceMock.Setup(s => s.CreateComment(It.IsAny<CreateCommentRequest>(), _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("You must be a participant of this project to perform this action."));

        var req = new CreateCommentRequest("Text", Guid.NewGuid());
        var result = await _controller.CreateComment(req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithNonexistentTicket_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CreateComment(It.IsAny<CreateCommentRequest>(), _authenticatedUserId))
            .ThrowsAsync(new TicketNotFoundException(ticketId));

        var req = new CreateCommentRequest("Text", ticketId);
        var result = await _controller.CreateComment(req);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCommentsByTicketId_ReturnsOkWithList()
    {
        var ticketId = Guid.NewGuid();
        var dtos = new List<CommentDto>
        {
            MakeDto(Guid.NewGuid(), "C1", ticketId, Guid.NewGuid()),
            MakeDto(Guid.NewGuid(), "C2", ticketId, Guid.NewGuid())
        };
        _serviceMock.Setup(s => s.GetCommentsByTicketId(ticketId)).ReturnsAsync(dtos);

        var result = await _controller.GetCommentsByTicketId(ticketId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, Assert.IsType<List<CommentDto>>(okResult.Value).Count);
    }

    [Fact]
    public async Task GetCommentsByTicketId_WithNonexistentTicket_ReturnsNotFound()
    {
        var ticketId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetCommentsByTicketId(ticketId)).ThrowsAsync(new TicketNotFoundException(ticketId));

        var result = await _controller.GetCommentsByTicketId(ticketId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteComment_AsAuthor_ReturnsNoContent()
    {
        var commentId = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveComment(commentId, _authenticatedUserId)).ReturnsAsync(true);

        var result = await _controller.DeleteComment(commentId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteComment_AsNonAuthor_ReturnsForbidden()
    {
        var commentId = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveComment(commentId, _authenticatedUserId))
            .ThrowsAsync(new ForbiddenException("Only the comment's author can delete it."));

        var result = await _controller.DeleteComment(commentId);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_WithNonexistentComment_ReturnsNotFound()
    {
        var commentId = Guid.NewGuid();
        _serviceMock.Setup(s => s.RemoveComment(commentId, _authenticatedUserId))
            .ThrowsAsync(new CommentNotFoundException(commentId));

        var result = await _controller.DeleteComment(commentId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}