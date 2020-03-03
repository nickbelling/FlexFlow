using Microsoft.AspNetCore.Identity;

namespace FlexFlow.Data.Entities
{
    /// <summary>
    /// Represents a user's ability to perform a given function within FlexFlow.
    /// </summary>
    public class UserRole : IdentityRole<int>
    {
    }
}
