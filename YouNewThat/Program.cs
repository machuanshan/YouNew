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
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices(sc =>
                {
                    sc.AddSingleton<Metrics>();
                    sc.AddHostedService<RemoteProxy>();
                })
                .RunConsoleAsync();
        }
    }
}
