using Microsoft.AspNetCore.Identity;

namespace FlexFlow.Data.Entities
{
    public class User : IdentityUser<int>
    {
        public string DisplayName { get; set; }
    }
}
