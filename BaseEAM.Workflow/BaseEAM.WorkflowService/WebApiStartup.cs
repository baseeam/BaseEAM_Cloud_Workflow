/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Data;
using Owin;
using System.Web.Http;

namespace BaseEAM.WorkflowService
{
    public class WebApiStartup
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

            appBuilder.UseAutofacMiddleware(AutoFacConfig.Initialize(config));
            //appBuilder.UseAutofacWebApi(config);
            appBuilder.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);
            appBuilder.UseWebApi(config);

            //start StartupTask manually
            var auditLogStartupTask = new AuditLogStartupTask();
            auditLogStartupTask.Execute();
        }
    }
}
