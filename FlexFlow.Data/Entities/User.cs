using Microsoft.AspNetCore.Identity;

namespace FlexFlow.Data.Entities
{
    /// <summary>
    /// Represents a single FlexFlow user.
    /// </summary>
    public class User : IdentityUser<int>
    {
        /// <summary>
        /// The user's human-readable display name.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
