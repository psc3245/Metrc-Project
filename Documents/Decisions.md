# Decisions & Scope Cuts

This project makes a number of deliberate scope cuts. Each one below was a
conscious choice given the timeline, not an oversight - and each has a clear
answer for "what would you do differently with more time."

## Fields and features deferred entirely

**No `Email` field on `User`.** 
Email was left out because I didn't see any real benefit to adding it. 
Given more time, it may be used to validate users or reset passwords, but within the limited 
time schedule, these were out of scope. Thus, I decided to leave email out to keep the User
class as lightweight as possible. 

**Flat comments, no threading.** 
Comment threading was a deliberate to leave out due to the complexities it could cause.
Cyclical comments, what happens when the first comment in a thread gets deleted, and
different branches within a thread are all problems that would be introduced with comment threads.

**No standalone `TagController`.** 
Tags are created through the Ticket Controller, as they exist to represent
a label on a ticket and cannot be applied to anythign else. Since the scope
of a tag is so limited, I felt they did not need to become a fully accessible
resource with a controller. 

## Authorization scope

**Participant-based, not fully role-based.** 
Projects maintain a list of participants that govern access to the project
and its tickets. This was the simplest way to enforce participation based authorization,
but given more time, I would definitely implement different roles for each project that
would govern access to different functionality.

**Reads are not participant-gated.** 
Any authenticated user can `GET` any project or ticket, they just cannot 
edit them. This was a line drawn to allow users to see what others are working on
and keep authentication manageable without creating unnecessary gates to accees.


**Comment deletion is author-only, not participant-wide**
Only authors can delete comments. This was decided to make the comments
feel like a way to share thoughts on the ticket from an individual standpoint.
Allowing anyone to modify comments would defeat the purpose and make the comment
section feel more like a shared document than a set of personal notes.

## Infrastructure / operational decisions

**JWT signing key is committed to `appsettings.Development.json` in plaintext.**
Deliberate for grading convenience - a reviewer running this locally shouldn't
have to hunt for secrets. In any real deployment this would come from
environment variables or a secrets manager (`dotnet user-secrets` locally, a
cloud secrets manager in production) and never from source control.