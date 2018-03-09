using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using JitBench;

namespace AllReady
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var jitBench = JitBenchHelper.Start();

            var config = new ConfigurationBuilder()
               .AddCommandLine(args)
               .AddEnvironmentVariables(prefix: "ASPNETCORE_")
               .Build();

            BuildWebHost(args, config).Start();

            jitBench.LogStartup();

            jitBench.MakeRequests("http://localhost:5000");

            jitBench.VerifyLibraryLocation();
        }

        public static IWebHost BuildWebHost(string[] args, IConfigurationRoot configRoot) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configRoot)
                .ConfigureAppConfiguration((ctx, config) => config.SetBasePath(ctx.HostingEnvironment.ContentRootPath).AddJsonFile("version.json"))
                .UseStartup<Startup>()
                .Build();
    }
}
