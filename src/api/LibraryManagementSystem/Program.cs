﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryManagementSystem.API
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        // public static async Task Main(string[] args)
        // {
        //     IHost host = CreateHostBuilder(args).Build();
        //     using var scope = host.Services.CreateScope();
        //     var services = scope.ServiceProvider;
        // }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                });
        //.ConfigureLogging(logging =>
        //{
        //    logging.ClearProviders();
        //})
        //.UseStartup<Startup>();
        // Todo Fix login
    }
}
