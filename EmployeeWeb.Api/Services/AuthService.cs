using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using EmployeeWeb.Api.Models.Entities;

namespace EmployeeWeb.Api.Services;

/// <summary>
/// Handles password hashing (BCrypt) and JWT token generation.
/// </summary>
public static class AuthService
{
    private static string GetJwtSecret(IConfiguration config)
    {
        var secret = config["Jwt:Secret"] ?? config["Jwt__Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Jwt:Secret is not configured. Add Jwt:Secret in appsettings.json.");
        return secret;
    }

    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public static string GenerateJwt(EmployeeEntity user, IConfiguration config)
    {
        var secret = GetJwtSecret(config);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.StaffEmail),
            new Claim(ClaimTypes.Name, user.StaffName ?? ""),
            new Claim("role", user.Role ?? "Employee"),
            new Claim("staffId", user.StaffID ?? "")
        };

        var expiry = DateTime.UtcNow.AddDays(7);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "EmployeeWebApi",
            audience: config["Jwt:Audience"] ?? "EmployeeWeb",
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
