﻿using System;
using System.Linq.Custom;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Reusable.Apps.Server.Json;
using Reusable.AspNetCore.Http;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Utilities.AspNetCore;
using Reusable.Utilities.AspNetCore.Hosting;
using Reusable.Utilities.NLog.LayoutRenderers;

[assembly: AspMvcViewLocationFormat("/src/Views/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("/src/Views/Shared/{0}.cshtml")]

namespace Reusable.Apps.Server
{
    [UsedImplicitly]
    public class Startup
    {
        //public const string HeaderPrefix = "X-Vault-";       

        private readonly IConfiguration _configuration;

        private readonly IHostingEnvironment _hostingEnvironment;

        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
            var builder = new ConfigurationBuilder()
                .SetBasePath(_hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{_hostingEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            _configuration = builder.Build();
        }



        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            SmartPropertiesLayoutRenderer.Register();

            // Add framework services.
            services
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new JsonStringConverter());
                });

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new RelativeViewLocationExpander("src"));
            });

            services.AddSingleton(_hostingEnvironment.ContentRootFileProvider);

            //services.AddApiVersioning(setupAction =>
            //{
            //    setupAction.ApiVersionReader = new HeaderApiVersionReader(HeaderFieldName.ApiVersion);
            //});;

            services.AddSingleton<IConfiguration>(_configuration);
            services.AddSingleton<ILoggerFactory>(
                new LoggerFactory()
                    .UseSemanticExtensions(_hostingEnvironment.EnvironmentName, "Reusable.Apps.Server")
                    .AddObserver<NLogRx>()
            );

            //services.AddScoped(serviceProvider => new ClientInfo(HeaderPrefix, serviceProvider.GetService<IMultipartName>()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory)
        {
            //loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            //loggerFactory.AddDebug();      



            app.UseSemanticLogger(config =>
            {
                config.MapProduct = _ => "Master_SemLog3";
            });

            //app.UseWhen(
            //    httpContext => !httpContext.Request.Method.In(new[] { "GET" }, StringComparer.OrdinalIgnoreCase),
            //    appBranch =>
            //    {
            //        var headers = new[]
            //        {
            //            HeaderNames.Product,
            //            HeaderNames.Profile
            //        }
            //        .Select(header => $"X-{header}");

            //        appBranch.UseHeaderValidator(headers);
            //    }
            //);

            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            if (env.IsDevelopmentAny())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unhandeled fault happened.");
                    });
                });
            }
        }
    }
}
