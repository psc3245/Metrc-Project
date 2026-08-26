# Architecture

## Layering

```
Controller -> Service -> Repository -> Database (EF Core / Postgres)
```

Every repository and service inherits from an interface to allow unit testing
to be possible. The testing suite mocks service and repository elements to simulate
real production usage. This also keeps the tests separate from the production datbase 
and allows us to not worry about muddying the two. Repository testing spins up a temporary
In-Memory database, so separation between the DB and testing is maintained. 

## DTOs as the wire boundary

Every entity has a corresponding DTO (`UserDto`, `ProjectDto`, `TicketDto`,
`CommentDto`) constructed directly from the entity (`new ProjectDto(project)`).
No entity is ever returned directly from a controller. Two reasons:

1. Entities have bidirectional navigation properties (`User.Projects` <->
   `Project.Participants`, etc.) which would blow up with a circular-reference
   exception if serialized directly.
2. It gives an explicit contract for what the API exposes - e.g. `User.PasswordHash`
   is never at risk of being serialized out, because `UserDto` simply doesn't
   have that field, rather than relying on remembering to add `[JsonIgnore]`.

One deliberate tradeoff worth naming: these DTOs are *write-only* by design -
they exist to be serialized out, not deserialized back in. Since they only
expose a constructor taking the entity, they can't be used for request bodies
(intentionally - request shapes are separate `CreateXRequest`/`UpdateXRequest`
records). 

## Authentication

JWT bearer tokens, issued on `/api/Auth/login` and `/api/Auth/signup`, no
refresh tokens. Password hashing uses BCrypt; the plaintext password is only
ever in transit over HTTPS, never stored or logged.

Every controller other than `AuthController` requires authentication by
default via a global fallback policy in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

## Project Access Authorization: participant-based, not role-based

Beyond authentication, most mutating endpoints enforce that the caller is a
**participant** of the relevant project:

- Creating/updating/deleting a project requires the caller to already be a
  participant. The project creator is automatically added as a participant on
  creation - without this, nobody, including the creator, could manage a
  project they just made.
- Creating, updating, deleting, assigning, tagging, or commenting on a ticket
  requires the caller to be a participant of that ticket's parent project.
- Assigning a ticket additionally requires the *assignee* to also be a
  project participant - you can't hand work to someone who isn't on the team.

**Deliberate exception:** deleting a *comment* is restricted to the comment's
own author, not any project participant - a departure from the
"any participant can touch shared resources" rule used everywhere else.
Comments are personal statements; tickets and projects are shared work items.

**Deliberate scope limit:** read endpoints (`GET`) are authenticated but not
participant-gated - any logged-in user can view any project or ticket. Only
writes are restricted.

Identity for `AuthorId` (Ticket) and `CommenterId` (Comment) is always derived
from the caller's JWT claims (`ClaimTypes.NameIdentifier`, falling back to the
raw `sub` claim), never accepted from the request body. A client should not be
able to claim a resource was authored by someone else.

## Error handling

Custom exceptions (`UserNotFoundException`, `ProjectNotFoundException`,
`TicketNotFoundException`, `CommentNotFoundException`, `UserExistsException`,
`BadLoginException`, `ForbiddenException`) carry semantic meaning and are
mapped to HTTP status codes in two places: a global exception-handling
middleware in `Program.cs` (catch-all, defense in depth) and per-action
`try/catch` blocks in controllers (explicit, testable at the controller-test
layer). Having both is deliberately redundant - the global handler guarantees
nothing slips through as a raw 500, while the local catches make each
controller's behavior visible and unit-testable without needing the full
middleware pipeline running.

## Data layer notes

- **Enums stored as strings.** Both `Priority` and `TicketStatus` use
  `HasConversion<string>()` in the DbContext, so the database is human-readable
  without cross-referencing code (`'HIGH'` rather than `2`). The JSON API
  matches this via a global `JsonStringEnumConverter`, so the wire format and
  the storage format agree.
- **UTC normalization.** A global value converter forces every `DateTime` and
  `DateTime?` property to UTC on read/write, since Npgsql requires consistent
  `timestamptz` handling. Two separate converters are needed (one for
  `DateTime`, one for `DateTime?`) - a single converter applied to both throws
  at model-build time, since EF requires the converter's type to exactly match
  the property's type.
- **Computed `Project.Status` requires eager loading.** `Project.GetStatus()`
  reads `Tickets.Count`/`Tickets.All(...)` directly off the in-memory
  collection. If a query fetches a `Project` without `.Include(p => p.Tickets)`,
  EF Core doesn't throw - it silently returns an empty list, and `GetStatus()`
  confidently reports `NOT_STARTED` for a project that may actually be well
  underway. The repository layer always includes `Tickets` and `Participants`
  before constructing a `ProjectDto` specifically to avoid this; it's a good
  illustration of why "loads without error" isn't the same as "loads correctly."

## Testing architecture

Three independent layers, each catching different classes of bug:

1. **Unit tests** (repository via EF InMemory, service via Moq, controller via
   Moq) - fast, isolated, prove logic without needing the app to actually run.
2. **Integration tests** (`Microsoft.AspNetCore.Mvc.Testing` /
   `WebApplicationFactory<Program>`) - spin up the real ASP.NET Core pipeline
   (routing, JWT middleware, JSON serialization, exception handling) against
   an in-memory database, proving things unit tests structurally can't: that
   an expired or malformed JWT is actually rejected by the real middleware,
   that model-validation attributes actually produce 400s through real model
   binding, that cascade deletes actually fire.