using System;
using System.Threading.Tasks;
using FlexFlow.Api.Database;
using FlexFlow.Api.Identity.TokenProviders;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlexFlow.Api
{
    public class Startup
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FlexFlow.Api.Startup"/> class.
        /// </summary>
        /// <param name="logger">The class logger.</param>
        public Startup(ILogger<Startup> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The service collection to add services to which will be made available throughout
        /// the application via dependency injection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("Configuring...");

            _logger.LogInformation("Configuring database...");
            // Add a database context pool. Contexts injected into controllers will be instances made available by the
            // pool and reused if necessary.
            services.AddDbContextPool<FlexFlowContext>(options =>
            {
                // Use SQLite for the database.
                string sqliteDbName = "FlexFlow.db";

                _logger.LogInformation("Setting SQLite to use the database {DbName}...", sqliteDbName);
                options.UseSqlite($"Filename={sqliteDbName}");
            });
            _logger.LogInformation("Database configured.");

            // Add ASP.NET Core Identity for user and user role management.
            _logger.LogInformation("Configuring ASP.NET Core Identity...");
            services
                .AddIdentity<User, UserRole>(options =>
                {
                    // Password options
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 5;
                    options.Password.RequiredUniqueChars = 1;

                    // Require a confirmed email
                    options.SignIn.RequireConfirmedEmail = true;

                    // Token providers (these are just the names of the ones we are using, they will be added below
                    // and configured later)
                    options.Tokens.EmailConfirmationTokenProvider = nameof(EmailConfirmationTokenProvider);
                    options.Tokens.PasswordResetTokenProvider = nameof(PasswordResetTokenProvider);
                })
                // Store users and various Identity information in the FlexFlow database context
                .AddEntityFrameworkStores<FlexFlowContext>()
                // Add token providers
                .AddTokenProvider<EmailConfirmationTokenProvider>(nameof(EmailConfirmationTokenProvider))
                .AddTokenProvider<PasswordResetTokenProvider>(nameof(PasswordResetTokenProvider));

            // Configure the token providers added earlier
            _logger.LogInformation("Configuring ASP.NET Core Identity token providers...");
            services
                .Configure<EmailConfirmationTokenProviderOptions>(options =>
                {
                    // Expire the email confirmation token after 1 day
                    options.TokenLifespan = TimeSpan.FromDays(1);
                })
                .Configure<PasswordResetTokenProviderOptions>(options =>
                {
                    // Expire the password reset token after 1 hour
                    options.TokenLifespan = TimeSpan.FromHours(1);
                });
            _logger.LogInformation("Identity configured.");

            services.AddMvc();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
        /// <param name="ctx">The database context.</param>
        public void Configure(
            IApplicationBuilder app, 
            IHostingEnvironment env,
            UserManager<User> userManager,
            FlexFlowContext ctx)
        {
            if (env.IsDevelopment())
            {
                _logger.LogInformation("Running in development mode. Using developer exception pages...");
                app.UseDeveloperExceptionPage();
            }

            // Set up cookie auth
            _logger.LogInformation("Configuring authentication middleware...");
            app.UseCookiePolicy();
            app.UseAuthentication();
            _logger.LogInformation("Authentication middleware configured.");

            // Configure database
            _logger.LogInformation("Configuring the database...");
            ConfigureDatabaseAndSeedUsers(ctx, userManager).Wait();
            _logger.LogInformation("Database configured and contactable.");

            app.UseMvc();
        }

        /// <summary>
        /// Ensures the database exists, migrates it to the latest schema and seeds it with user roles and an 
        /// administrator user.
        /// </summary>
        /// <returns></returns>
        /// <param name="context">The database context.</param>
        /// <param name="userManager">The Identity user manager.</param>
        private async Task ConfigureDatabaseAndSeedUsers(FlexFlowContext context, UserManager<User> userManager)
        {
            // Ensure the database is created and any migrations are applied
            _logger.LogInformation("Ensuring the database is created...");
            await context.Database.MigrateAsync();

            // Create the admin user if it does not exist
            if (await userManager.FindByNameAsync("admin") == null)
            {
                _logger.LogInformation("Administrator user does not exist. Attempting to create it...");

                IdentityResult result = await userManager.CreateAsync(new User
                {
                    UserName = "admin",
                    DisplayName = "Administrator"
                }, "admin");

                if (result.Succeeded)
                {
                    _logger.LogInformation("Administrator user created successfully.");
                }
                else
                {
                    _logger.LogError("Could not create the Administrator user. Reason: {Reason}", result);
                }
            }
            else
            {
                _logger.LogInformation("Administrator user already exists, no need to create.");
            }
        }
    }
}
