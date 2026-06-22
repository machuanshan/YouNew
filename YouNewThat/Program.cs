using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using YouNewAll;

namespace YouNewThat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSingleton<Metrics>();
            builder.Services.AddHostedService<RemoteProxy>();
            await builder.Build().RunAsync();
        }
    }
}
