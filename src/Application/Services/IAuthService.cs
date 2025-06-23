using System.Security.Claims;

namespace Application.Services;

public interface IAuthService
{
    string GenerateJwtToken(int userId, string email, string role);
    ClaimsPrincipal? ValidateJwtToken(string token);
    int? GetUserIdFromToken(string token);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}