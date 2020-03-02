using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlexFlow.Api.Database
{
    /// <summary>
    /// The FlexFlow database context. Note that this inherits from <see cref="IdentityDbContext"/> as opposed to the regular EF <see cref="DbContext"/>.
    /// This ensures that Users, UserRoles and other Identity-related items are stored in this database.
    /// </summary>
    public class FlexFlowContext : IdentityDbContext<User, UserRole, int>
    {
        public FlexFlowContext(DbContextOptions<FlexFlowContext> options)
            : base(options)
        { }
    }
}
