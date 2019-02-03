﻿using System.Collections.Generic;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;
using ServiceStack.Mvc;
using ServiceStack.Validation;

namespace CheckWebCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost
            {
                AppSettings = new NetCoreAppSettings(Configuration)
            });
        }
    }
    
    public class AppHost : AppHostBase
    {
        public AppHost() : base("TestLogin", typeof(MyServices).Assembly) { }


        // http://localhost:5000/auth/credentials?username=testman@test.com&&password=!Abc1234
        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            Plugins.Add(new TemplatePagesFeature()); // enable server-side rendering, see: http://templates.servicestack.net

            if (Config.DebugMode)
            {
                Plugins.Add(new HotReloadFeature());
            }

            Plugins.Add(new RazorFormat()); // enable ServiceStack.Razor
            
            SetConfig(new HostConfig
            {
                AddRedirectParamsToQueryString = true,
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false),
                UseSameSiteCookies = true,
            });

            container.Register<ICacheClient>(new MemoryCacheClient());
            
            Plugins.Add(new ValidationFeature());
        }
    }
    
    [Exclude(Feature.Metadata)]
    [FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
    public class FallbackForClientRoutes
    {
        public string PathInfo { get; set; }
    }
    
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    [Route("/testauth")]
    public class TestAuth : IReturn<TestAuth> {}

    [Route("/session")]
    public class Session : IReturn<AuthUserSession> {}

    //    [Authenticate]
    public class MyServices : Service
    {
        //Return index.html for unmatched requests so routing is handled on client
        public object Any(FallbackForClientRoutes request) => 
            Request.GetPageResult("/");
//            new PageResult(Request.GetPage("/")) { Args = { [nameof(Request)] = Request } };

        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }

        public object Any(TestAuth request) => request;

        [Authenticate]
        public object Any(Session request) => SessionAs<AuthUserSession>();
        
        private static readonly byte[] TransparentGif =
        {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02
        };

        public byte[] Any(TransGif request) => TransparentGif;

        [AddHeader(ContentType="image/gif")]
        public byte[] Any(TransGif2 request) => TransparentGif;

        public object Any(TransGif3 request) => 
            new HttpResult(TransparentGif) { ContentType = MimeTypes.ImageGif };

        [Route("/gif")]
        public class TransGif {}

        [Route("/gif2")]
        public class TransGif2 {}
        [Route("/gif3")]
        public class TransGif3 {}
    }
}