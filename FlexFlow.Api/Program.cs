using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;

namespace FlexFlow.Api
{
    public class Program
    {
        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // Start
            try
            {
                Log.Information("Starting FlexFlow...");

                // Build and start the web host
                IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>()
                    .UseSerilog();

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
