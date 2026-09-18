using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LPUPlus.Server.Auth;

/// <summary>
/// Generates and validates short-lived JWT tokens for session authentication.
/// Tokens are issued after successful pairing and used for all subsequent API calls.
/// </summary>
public sealed class JwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TimeSpan _tokenLifetime;

    public JwtTokenService(string? signingSecret = null, TimeSpan? lifetime = null)
    {
        _issuer = "lpuplus-server";
        _audience = "lpuplus-client";
        _tokenLifetime = lifetime ?? TimeSpan.FromHours(1);

        // Generate a strong signing key if not provided
        if (signingSecret is null)
        {
            var keyBytes = new byte[64];
            RandomNumberGenerator.Fill(keyBytes);
            signingSecret = Convert.ToBase64String(keyBytes);
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret));
    }

    /// <summary>
    /// Generate a session JWT token after successful pairing.
    /// </summary>
    public string GenerateSessionToken(string sessionId, string deviceId, string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sessionId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("device_id", deviceId),
        };

        // Add permissions as individual claims
        foreach (var perm in permissions)
        {
            claims.Add(new Claim("perm", perm));
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(_tokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate a session token and extract claims.
    /// Returns null if the token is invalid or expired.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the token validation parameters for ASP.NET Core authentication middleware.
    /// </summary>
    public TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
