using System;
using CoreWCF.Configuration;
using CoreWCF.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CoreWCF;
using Cupid.Models;

namespace Cupid.Server
{
    internal class HostProgram
    {
        static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddServiceModelServices();
            builder.Services.AddServiceModelMetadata();

            builder.Services.AddSingleton<CupidService>();
            var app = builder.Build();

            var appBuilder = (IApplicationBuilder)app;
            appBuilder.UseServiceModel(serviceBuilder =>
            {
                serviceBuilder.AddService<CupidService>();
                serviceBuilder.AddServiceEndpoint<CupidService, ICupidService>(new NetTcpBinding(SecurityMode.None), "/CupidService");
            });

            var svc = app.Services.GetRequiredService<CupidService>();
            svc.Start(); // starts with 60s delay per spec

            Console.WriteLine("Cupid server running on net.tcp://localhost:9000/CupidService");
            app.Run("net.tcp://localhost:9000");
        }
    }
}
