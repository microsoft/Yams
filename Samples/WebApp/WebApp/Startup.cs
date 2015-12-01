using System;
using System.Web.Http;
using Owin;

namespace WebApp
{
    class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnsureInitialized();
            appBuilder.UseWebApi(config);

            Console.WriteLine("Available Apis:");
            foreach (var api in config.Services.GetApiExplorer().ApiDescriptions)
            {
                Console.WriteLine("{0} {1}", api.HttpMethod, api.RelativePath);
            }
        } 
    }
}
