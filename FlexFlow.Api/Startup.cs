using System;
using System.Threading.Tasks;
using FlexFlow.Api.Database;
using FlexFlow.Api.Identity.TokenProviders;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlexFlow.Api
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FlexFlow.Api.Startup"/> class.
        /// </summary>
        /// <param name="logger">The class logger.</param>
        public Startup(ILogger<Startup> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The service collection to add services to which will be made available throughout
        /// the application via dependency injection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.LogInformation("Configuring database...");

            // Add a database context pool. Contexts injected into controllers will be instances made available by the
            // pool and reused if necessary.
            services.AddDbContextPool<FlexFlowContext>(options =>
            {
                // Use SQLite for the database.
                string sqliteDbName = _config[Constants.CONFIG_SQLITEDATABASE];

                _logger.LogInformation("Setting SQLite to use the database {DbName}...", sqliteDbName);
                options.UseSqlite($"Filename={sqliteDbName}");
            });
            _logger.LogInformation("Database configured.");

            // Add ASP.NET Core Identity for user and user role management.
            _logger.LogInformation("Configuring ASP.NET Core Identity...");
            services
                .AddIdentity<User, UserRole>(options =>
                {
                    // Require a confirmed email
                    options.SignIn.RequireConfirmedEmail = true;

                    // Set lockout options
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

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

            // Set up cookie authentication
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;

                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });

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

            app.UseAuthentication();

            // Condenses ASP.NET Core's logs into a single line instead of 5+ lines for every single API request
            app.UseSerilogRequestLogging();

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

                User admin = new User
                {
                    UserName = "admin",
                    DisplayName = "Administrator",
                    Email = _config[Constants.CONFIG_ADMINEMAIL]
                };

                IdentityResult result = await userManager.CreateAsync(admin, "admin");

                if (result.Succeeded)
                {
                    _logger.LogInformation("Administrator user created successfully.");
                    string emailToken = await userManager.GenerateEmailConfirmationTokenAsync(admin);
                    await userManager.ConfirmEmailAsync(admin, emailToken);
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
