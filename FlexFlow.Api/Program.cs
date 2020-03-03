using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace FlexFlow.Api
{
    /// <summary>
    /// The entry class of the program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            // Set up configuration files
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                // Use appsettings.json file
                .AddJsonFile("appsettings.json", false, true)
                // Allow override using environment variables
                .AddEnvironmentVariables("FlexFlow")
                .Build();

            // Set up global logger configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // See "app.UseSerilogRequestLogging()" in Startup.cs
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("flexflow.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Start
            try
            {
                Log.Information("Starting FlexFlow...");

                // Build and start the web host
                IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args)
                    .UseConfiguration(config)
                    .UseSerilog()
                    .UseStartup<Startup>();

                IWebHost webHost = builder.Build();
                webHost.Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while attempting to start FlexFlow.");
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application exit 
                // (also avoids segmentation fault on Linux)
                Log.CloseAndFlush();
            }
        }
    }
}
