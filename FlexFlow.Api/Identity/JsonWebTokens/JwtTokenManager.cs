using FlexFlow.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FlexFlow.Api.Identity.JsonWebTokens
{
    /// <inheritdoc cref="IJwtTokenManager"/>
    public class JwtTokenManager : IJwtTokenManager
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly SigningCredentials _signingCredentials;
        private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
        private readonly int _tokenLifeMinutes = 30;
        private readonly string _audience;
        private readonly string _issuer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="cache"></param>
        public JwtTokenManager(IConfiguration config, IMemoryCache cache)
        {
            _config = config;
            _cache = cache;

            _audience = _config[Constants.Config.BEARER_AUDIENCE];
            _issuer = _config[Constants.Config.BEARER_ISSUER];

            // Create and store the JWT signing key
            byte[] key = Encoding.UTF8.GetBytes(_config[Constants.Config.BEARER_SECRET]);
            _signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature);

            // Update the token expiry life if it's configured
            if ( int.TryParse( _config[Constants.Config.BEARER_LIFETIME_MINUTES], out int minutes ) )
            {
                _tokenLifeMinutes = minutes;
            }
        }

        /// <inheritdoc />
        public Task<string> GenerateTokenAsync(string username, string email, IList<string> roles)
        {
            // Create the token descriptor
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(BuildClaims(username, email, roles)),
                Expires = DateTime.UtcNow.AddMinutes(_tokenLifeMinutes),
                SigningCredentials = _signingCredentials,
                Audience = _audience,
                Issuer = _issuer
            };

            // Sign the descriptor with our secret key and generate the token
            SecurityToken token = _tokenHandler.CreateToken(tokenDescriptor);

            // Return the token written in JWT format (basically base64'd as a string with the signature at the end)
            return Task.FromResult(_tokenHandler.WriteToken(token));
        }

        /// <inheritdoc />
        public Task<bool> IsBlacklistedAsync(string token)
        {
            bool isInBlacklistCache = _cache.Get(GetBlacklistKey(token)) != null;
            return Task.FromResult(isInBlacklistCache);
        }

        /// <inheritdoc />
        public Task BlacklistAsync(string token)
        {
            // Add this token to the Blacklist cache. It's storing a regular object (that'll never be accessed; it's more about storing *something*).
            _cache.Set<byte>(
                GetBlacklistKey(token),                     // Generate the key ("JwtTokenManager/blacklist/{token}")
                1,                                          // Store *something* (distributed caches doesn't work with storing null values, and we might want to upgrade later)
                TimeSpan.FromMinutes(_tokenLifeMinutes));   // Expire the blacklist at some point after the token would expire

            return Task.CompletedTask;
        }

        /// <summary>
        /// Build the claims which will be stored in the token.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        private static List<Claim> BuildClaims(string username, string email, IList<string> roles)
        {
            List<Claim> claims = new List<Claim>(roles.Count + 2)
            {
                // Accessible in a Controller using User.Identity.Name
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email)
            };

            // Using these roles, endpoints will be restricted using [Authorize(Roles = "Admin")] for example
            foreach (string role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        /// <summary>
        /// Gets the key which will be stored in the MemoryCache that indicates this token is blacklisted.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private string GetBlacklistKey(string token) => $"{nameof(JwtTokenManager)}/blacklist/{token}";
    }
}
