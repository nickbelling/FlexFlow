using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFlow.Api.Identity.JsonWebTokens
{
    /// <inheritdoc cref="ICurrentJwtTokenManager"/>
    public class CurrentJwtTokenManager : ICurrentJwtTokenManager
    {
        private readonly IHttpContextAccessor _httpContext;
        private readonly IJwtTokenManager _tokenManager;

        public CurrentJwtTokenManager(IHttpContextAccessor httpContext, IJwtTokenManager tokenManager)
        {
            _httpContext = httpContext;
            _tokenManager = tokenManager;
        }

        public async Task<bool> IsBlacklistedAsync()
        {
            return await _tokenManager.IsBlacklistedAsync(GetCurrentAuthTokenAsync());
        }

        public async Task BlacklistAsync()
        {
            await _tokenManager.BlacklistAsync(GetCurrentAuthTokenAsync());
        }

        /// <summary>
        /// Pulls the current token out of the current HTTP request.
        /// </summary>
        /// <returns></returns>
        private string GetCurrentAuthTokenAsync()
        {
            string token = string.Empty;

            if ( _httpContext.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues authorizationHeader))
            {
                if ( authorizationHeader != StringValues.Empty)
                {
                    // Auth header is in the format "Bearer [token]"
                    token = authorizationHeader.Single().Split(" ").Last();
                }
            }

            return token;
        }
    }
}
