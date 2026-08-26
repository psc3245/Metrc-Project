# AI-Assisted Development Process

I used Claude as a coding assistant throughout this project, mainly for
generating the repetitive layers - repository, service, and controller code
following a pattern, plus test scaffolding. I reviewed and approved that
pattern closely on the first entity (User) before letting it repeat across
Project, Ticket, and Comment. That let me spend my own time on the stuff that
actually shapes the project: scope, architecture, and making sure things
actually worked.

## How I drove the process

I decided the build order (User → Project → Ticket → Comment), the overall
architecture (layered, interface-driven so it could be tested), and every
scope cut in this repo. A few examples where I made a real call instead of
just taking the default:

- When I had to decide how much authorization to build given the deadline, I
  went with full participant-based authorization instead of stopping at
  plain authentication - even though it meant reworking four services and
  rewriting a test suite that was already passing. Authorization is too
  central to a multi-user tool to skip, so I decided it was worth the time.
- I decided comment deletion should be author-only, while ticket editing
  stays open to any project participant. That's a product call about what
  kind of thing a comment is - personal - versus a ticket, which is shared.
  Nothing about the implementation forced that split; I chose it.
- I made every scope cut in `DECISIONS.md` deliberately, weighing what the
  assignment was actually grading against what I had time to build well.

## My verification discipline

I didn't accept a description of what code should do - I ran it, and only
moved on once I'd seen it actually pass. One example that stuck with me:

Reading through a generated controller, I noticed there was no reference to
auth or a token service anywhere in the file. I pushed back on that instead
of assuming it was fine. It turned out auth was being enforced globally
through a fallback policy elsewhere in the app - so it was working correctly,
just not visible from that file. I decided that mattered anyway: a reviewer
skimming one controller shouldn't have to already know about a separate
global policy to understand whether it's protected. So I had it made
explicit on every controller, even though it was technically redundant with
the global policy.

## Where AI assistance actually helped

Writing three-layer test suites by hand for four entities would have eaten
most of my time on repetition instead of substance. Getting that scaffolding
generated quickly let me spend my
attention where it counted: the authorization model, the scope decisions, and
confirming everything genuinely worked rather than just looked like it should.