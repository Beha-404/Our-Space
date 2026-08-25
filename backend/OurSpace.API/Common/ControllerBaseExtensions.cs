using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace OurSpace.API.Common;

public static class ControllerBaseExtensions
{
    public static int GetUserId(this ControllerBase controller)
    {
        var sub = controller.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.Parse(sub!);
    }
}
