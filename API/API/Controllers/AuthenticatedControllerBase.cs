using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// Shared by any controller that needs to know who's calling - e.g. to derive
// ticket authorship or check project participant membership. Never trust a
// caller id supplied in a request body; always derive it from the validated JWT.
public abstract class AuthenticatedControllerBase : ControllerBase
{
    protected Guid GetAuthenticatedUserId()
    {
        // ASP.NET Core's default inbound claim mapping rewrites a JWT's "sub"
        // claim to ClaimTypes.NameIdentifier; fall back to the raw "sub" claim
        // defensively in case that mapping is ever disabled (MapInboundClaims = false).
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (idClaim == null || !Guid.TryParse(idClaim, out var userId))
            throw new UnauthorizedAccessException("Could not determine the authenticated user's id from the token.");

        return userId;
    }
}