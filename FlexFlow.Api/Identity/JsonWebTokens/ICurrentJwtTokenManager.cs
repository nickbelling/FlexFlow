using System.Threading.Tasks;

namespace FlexFlow.Api.Identity.JsonWebTokens
{
    /// <summary>
    /// A transient (i.e. per-request) manager capable of blacklisting the current user's token (or checking if it's blacklisted).
    /// </summary>
    public interface ICurrentJwtTokenManager
    {
        /// <summary>
        /// Checks if the current token is blacklisted.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsBlacklistedAsync();

        /// <summary>
        /// Blacklists the current token.
        /// </summary>
        /// <returns></returns>
        Task BlacklistAsync();
    }
}
