using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AlfTekPro.IntegrationTests.Infrastructure;

public static class TestAuthHelper
{
    public const string JwtSecret = "ThisIsATestSecretKeyThatMustBeAtLeast32CharsLong!";
    public const string JwtIssuer = "AlfTekPro.Test";
    public const string JwtAudience = "AlfTekPro.Test.Audience";

    public static string GenerateToken(
        Guid userId,
        string email,
        string role,
        Guid? tenantId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("user_id", userId.ToString()),
            new("role", role)
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient CreateAuthenticatedClient(
        HrmsWebApplicationFactory factory,
        Guid userId,
        string email,
        string role,
        Guid? tenantId = null)
    {
        var client = factory.CreateClient();
        var token = GenerateToken(userId, email, role, tenantId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
