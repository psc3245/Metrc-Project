using API.Entities.Exceptions;
using API.Projects;

namespace API.Entities.HelperClasses;

// Shared by ProjectService and TicketService - a ticket's authorization is
// governed by its parent project's participant list, so both need this same check.
internal static class ProjectAuthorization
{
    public static void EnsureParticipant(Project project, Guid callerId)
    {
        if (project.Participants.All(p => p.Id != callerId))
            throw new ForbiddenException("You must be a participant of this project to perform this action.");
    }
}