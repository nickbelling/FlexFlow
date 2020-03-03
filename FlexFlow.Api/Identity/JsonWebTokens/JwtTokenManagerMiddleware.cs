using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

namespace FlexFlow.Api.Identity.JsonWebTokens
{
    /// <summary>
    /// Intercepts all incoming API requests and checks the token against the blacklist. If the token is present in the blacklist, rejects it.
    /// </summary>
    public class JwtTokenManagerMiddleware : IMiddleware
    {
        private readonly ILogger _logger;
        private readonly ICurrentJwtTokenManager _tokenManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tokenManager"></param>
        public JwtTokenManagerMiddleware(
            ILogger<JwtTokenManagerMiddleware> logger,
            ICurrentJwtTokenManager tokenManager)
        {
            _logger = logger;
            _tokenManager = tokenManager;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if ( await _tokenManager.IsBlacklistedAsync() )
            {
                // This token is blacklisted
                _logger.LogDebug("Provided token is blacklisted. Setting return status to Unauthorized...");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                // Allow fallthrough
                _logger.LogTrace("Token is null or not blacklisted. Continuing through middleware...");
                await next(context);
            }
        }
    }
}
