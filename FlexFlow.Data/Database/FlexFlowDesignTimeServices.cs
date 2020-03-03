using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;

namespace FlexFlow.Data.Database
{
    /// <summary>
    /// Serves up things that only affect the Database at Design Time (e.g. when making a migration)
    /// </summary>
    public class FlexFlowDesignTimeServices : IDesignTimeDbContextFactory<FlexFlowContext>, IDesignTimeServices
    {
        /// <summary>
        /// Overrides the <see cref="CSharpMigrationsGenerator"/> with the <see cref="FlexFlowMigrationsGenerator"/>. 
        /// This basically just calls the <see cref="CSharpMigrationsGenerator"/> but wraps it with pragmas to stop
        /// it complaining about missing XML comments in the migration.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureDesignTimeServices(IServiceCollection services)
        {
            // Override the CSharpMigrationsGenerator with the FlexFlow one
            services.AddSingleton<IMigrationsCodeGenerator, FlexFlowMigrationsGenerator>();
        }

        /// <summary>
        /// Configures and creates a design-time FlexFlow context.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public FlexFlowContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<FlexFlowContext> builder = new DbContextOptionsBuilder<FlexFlowContext>();

            // Just use a sample SQLite database
            builder.UseSqlite($"Filename=FlexFlow.db");

            return new FlexFlowContext(builder.Options);
        }
    }
}
