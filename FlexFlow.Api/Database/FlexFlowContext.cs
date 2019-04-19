using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlexFlow.Api.Database
{
    public class FlexFlowContext : IdentityDbContext<User, UserRole, int>
    {
        public FlexFlowContext(DbContextOptions<FlexFlowContext> options)
            : base(options)
        { }
    }
}
