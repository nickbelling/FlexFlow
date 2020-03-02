using FlexFlow.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFlow.Api.Identity.JsonWebTokens
{
    /// <summary>
    /// A process-wide JWT token manager. Is capable of generating tokens as well as blacklisting them.
    /// </summary>
    public interface IJwtTokenManager
    {
        /// <summary>
        /// Generates a valid JWT for the given user and roles.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        Task<string> GenerateTokenAsync(string username, string email, IList<string> roles);

        /// <summary>
        /// Checks if the given token is blacklisted.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> IsBlacklistedAsync(string token);

        /// <summary>
        /// Blacklists the given token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task BlacklistAsync(string token);
    }
}
