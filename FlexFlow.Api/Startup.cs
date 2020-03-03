using FlexFlow.Api.Identity.JsonWebTokens;
using FlexFlow.Common;
using FlexFlow.Data.Database;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FlexFlow.Api
{
    /// <summary>
    /// The class used to start up the web server, the dependency injection container, and register all of the required services.
    /// </summary>
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FlexFlow.Api.Startup"/> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
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
            // Add app-specific services
            AddDatabaseServices(services);
            AddIdentityServices(services);
            AddJwtServices(services);

            services.AddAuthorization();
            services.AddControllers();

            services.AddOpenApiDocument();
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
            IWebHostEnvironment env,
            UserManager<User> userManager,
            FlexFlowContext ctx)
        {
            if (env.IsDevelopment())
            {
                _logger.LogInformation("Running in development mode. Using developer exception pages...");
                app.UseDeveloperExceptionPage();
            }

            // Set up the ASP.NET Core routing, auth, middleware and set it up to find the Controllers
            app.UseRouting()
               .UseAuthentication()
               .UseAuthorization()
               .UseMiddleware<JwtTokenManagerMiddleware>()
               .UseEndpoints(configure => configure.MapControllers());

            // Condenses ASP.NET Core's logs into a single line instead of 5+ lines for every single API request
            app.UseSerilogRequestLogging();

            // Configure database
            _logger.LogInformation("Configuring the database...");
            ConfigureDatabaseAndSeedUsers(ctx, userManager).Wait();
            _logger.LogInformation("Database configured and contactable.");

            // Add Swagger document middlewares
            app.UseOpenApi(options => options.Path = "/openapi.json");

            app.UseReDoc(options =>
            {
                options.Path = "/redoc";
                options.DocumentPath = "/openapi.json";
            });
        }

        /// <summary>
        /// Adds the EF Database. It will be configured later.
        /// </summary>
        /// <param name="services"></param>
        private void AddDatabaseServices(IServiceCollection services)
        {
            _logger.LogInformation("Configuring database...");
            // Add a database context pool. Contexts injected into controllers will be instances made available by the
            // pool and reused if necessary.
            services.AddDbContextPool<FlexFlowContext>(options =>
            {
                // Use SQLite for the database.
                string sqliteDbName = _config[Constants.Config.SQLITE_DATABASE];

                _logger.LogInformation("Setting SQLite to use the database {DbName}...", sqliteDbName);
                options.UseSqlite($"Filename={sqliteDbName}");
            });
            _logger.LogInformation("Database configured.");
        }

        /// <summary>
        /// Configures the Identity provider, and ensures it's using sensible defaults.
        /// </summary>
        /// <param name="services"></param>
        private void AddIdentityServices(IServiceCollection services)
        {
            // Add ASP.NET Core Identity for user and user role management.
            _logger.LogInformation("Configuring ASP.NET Core Identity...");
            services
                .AddIdentity<User, UserRole>(options =>
                {
                    // Require a confirmed email
                    options.SignIn.RequireConfirmedEmail = true;

                    // Remove all of the dumb default restrictions that are on Identity passwords
                    options.Password.RequiredLength = 0;
                    options.Password.RequireDigit = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;

                    // Set lockout options
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                })
                // Store users and various Identity information in the FlexFlow database context
                .AddEntityFrameworkStores<FlexFlowContext>()
                // Using our own Identity options means the Default Token Providers aren't used; add them back
                // TODO - maybe replace these with our own again
                .AddDefaultTokenProviders();
        }

        /// <summary>
        /// Configures the JWT middleware.
        /// </summary>
        /// <param name="services"></param>
        private void AddJwtServices(IServiceCollection services)
        {
            // Memory cache used for storing blacklisted tokens
            services.AddSingleton<IMemoryCache, MemoryCache>();

            // HTTP Context Accessor used by the TokenManagerMiddleware to get the token out and check it against the blacklist
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // TokenManager is singleton (one for the entire process), CurrentTokenManager is transient (one per request)
            services.AddSingleton<IJwtTokenManager, JwtTokenManager>();
            services.AddTransient<ICurrentJwtTokenManager, CurrentJwtTokenManager>();

            // Add the middleware that validates the tokens
            services.AddTransient<JwtTokenManagerMiddleware>();

            // Pull the JWT signing key secret from the config
            string secret = _config[Constants.Config.BEARER_SECRET];
            byte[] key = Encoding.UTF8.GetBytes(secret);

            // Set up JWT
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateActor = false,
                    ValidateIssuer = true,
                    ValidIssuer = _config[Constants.Config.BEARER_ISSUER],
                    ValidateAudience = true,
                    ValidAudience = _config[Constants.Config.BEARER_AUDIENCE]
                };
            });
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
                    Email = _config[Constants.Config.ADMIN_EMAIL]
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
