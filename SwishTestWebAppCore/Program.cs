﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SwishTestWebAppCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().ConfigureAppConfiguration((ctx, builder) =>
                {
                    var config = builder.Build();
                    var vault = config["KeyVault:BaseUrl"];
                    if (!string.IsNullOrWhiteSpace(vault))
                    {
                        var tokenProvider = new AzureServiceTokenProvider();
                        var kvClient = new KeyVaultClient((authority, resource, scope) =>
                            tokenProvider.KeyVaultTokenCallback(authority, resource, scope));
                        builder.AddAzureKeyVault(vault, kvClient, new DefaultKeyVaultSecretManager());
                    }
                });
    }
}
