﻿using System;
using System.IO;
using LiGet.Configuration;
using LiGet.Entities;
using LiGet.Extensions;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.AspNetCore.Http.Features;

namespace LiGet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureLiGet(Configuration, httpServices: true);

            services.AddSpaStaticFiles(configuration =>
            {
                var sourceDist = Path.Combine(Environment.CurrentDirectory, "src", "LiGet.UI" , "dist");
                if(Directory.Exists(sourceDist))
                {
                    // we are running tests from root of project
                    configuration.RootPath = sourceDist;
                    return;
                }
                sourceDist = Path.Combine("src", "LiGet.UI" , "dist");
                for(int depth=0; depth < 5; depth++) {
                    sourceDist = Path.Combine("..", sourceDist);
                    if(Directory.Exists(sourceDist))
                    {
                        // we are running tests from debugger/editor
                        configuration.RootPath = sourceDist;
                        return;
                    }
                }
                // In production, the UI files will be served from wwwroot directory next to LiGet.dll file
                string codeBase = Path.GetDirectoryName(typeof(Startup).Assembly.CodeBase);
                configuration.RootPath = Path.Combine(codeBase, "wwwroot");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var scopeFactory = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }

            // Run migrations if enabled
            var databaseOptions = app.ApplicationServices.GetRequiredService<IOptions<LiGetOptions>>()
                    .Value
                    .Database;
            if(databaseOptions.RunMigrations) {
                using (var scope = scopeFactory.CreateScope())
                {
                    scope.ServiceProvider
                        .GetRequiredService<IContext>()
                        .Database
                        .Migrate();
                }
            }

            app.UseForwardedHeaders();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseCors(ConfigureCorsOptions.CorsPolicy);

            app.UseCarter();

            app.Use(async (context, next) =>
            {
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
                await next.Invoke();
            });
            
            if(env.IsDevelopment())
            {
                app.UseSpa(spa =>
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:1234");
                });
            }
        }
    }
}
