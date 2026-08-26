# Entity Plan (As-Built)

This document describes each core entity and their fields. 

All entities inherit from `BaseEntity`:
* `Id` - Guid
* `CreatedAt` - DateTime (UTC)
* `UpdatedAt` - DateTime (UTC)

## User
* `Username` - string, unique
* `PasswordHash` - string (BCrypt hash)
* `Projects[]` - many-to-many with Project (`ProjectParticipants` join table)
* `AssignedTickets[]` - Tickets where this user is the assignee
* `AuthoredTickets[]` - Tickets where this user is the author
* `Comments[]` - Comments authored by this user

## Project
* `Title` - string, required, non-empty
* `Description` - string?
* `Deadline` - DateTime?
* `Participants[]` - many-to-many with User (`ProjectParticipants` join table).
  The creator is automatically added as a participant on creation.
* `Tickets[]` - one-to-many, cascade delete (deleting a project deletes its tickets)
* `Status` - computed, not persisted: `NOT_STARTED` | `IN_PROGRESS` | `COMPLETED`,
  derived from the state of `Tickets`

## Ticket
* `Title` - string, required, non-empty
* `Description` - string?
* `Deadline` - DateTime?
* `Priority` - enum: `LOW` | `MEDIUM` | `HIGH` (stored as string in Postgres,
  serialized as string over JSON)
* `Status` - enum: `TO_DO` | `IN_PROGRESS` | `IN_REVIEW` | `COMPLETED` (stored/
  serialized as string)
* `ProjectId` / `Project` - required, cascade delete from Project
* `AssigneeId` / `Assignee` - User?, set null on user delete (unassigns rather
  than deleting the ticket)
* `AuthorId` / `Author` - User, required. Always derived from the authenticated
  caller's JWT claims on creation - never accepted from the request body
* `Tags[]` - many-to-many with Tag (`TicketTags` join table)
* `Comments[]` - one-to-many, cascade delete (deleting a ticket deletes its comments)

## Comment
* `Text` - string, required, non-empty
* `TicketId` / `Ticket` - required, cascade delete from Ticket
* `CommenterId` / `Commenter` - User, required. Like `Ticket.AuthorId`, always
  derived from the authenticated caller's JWT, never from the request body

## Tag
* `Name` - string, unique (case-insensitive; enforced by a unique DB index plus
  an application-level get-or-create with race handling - see `ARCHITECTURE.md`)
* `Color` - string, hex code (e.g. `#FF0000`)
* `Tickets[]` - many-to-many with Ticket (`TicketTags` join table)
* Not independently CRUD-able via its own controller - tags are only created,
  attached, and removed through Ticket endpoints (see `DECISIONS.md`)

## Enum storage note
Both `Priority` and `TicketStatus` are stored as strings in Postgres (via
`HasConversion<string>()`) rather than the EF default integer encoding, so the
database is human-readable without cross-referencing code. They are also
serialized as strings over the JSON API via a global `JsonStringEnumConverter`,
so what you see in the database matches what you see over the wire.