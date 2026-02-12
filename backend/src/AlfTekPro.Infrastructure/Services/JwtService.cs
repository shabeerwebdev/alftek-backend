using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Implementation of JWT token generation and validation service
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secret = configuration["JWT:Secret"]
            ?? throw new InvalidOperationException("JWT:Secret is not configured");
        _issuer = configuration["JWT:Issuer"]
            ?? throw new InvalidOperationException("JWT:Issuer is not configured");
        _audience = configuration["JWT:Audience"]
            ?? throw new InvalidOperationException("JWT:Audience is not configured");
        _expiryMinutes = int.Parse(configuration["JWT:ExpiryMinutes"] ?? "60");
    }

    /// <summary>
    /// Generates a JWT access token with user claims
    /// </summary>
    public string GenerateAccessToken(Guid userId, string email, string role, Guid? tenantId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("user_id", userId.ToString()),
            new Claim("role", role)
        };

        // Add tenant_id claim for tenant-scoped users (not SuperAdmin)
        if (tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT token and extracts the user ID
    /// </summary>
    public Guid? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract user ID from claims
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            // Token validation failed
            return null;
        }
    }
}
