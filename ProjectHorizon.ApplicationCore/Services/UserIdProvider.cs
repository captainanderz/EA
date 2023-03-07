using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class UserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
    }
}