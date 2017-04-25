using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Arbor.X.Tests.DummyWebApplication
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.MapMvcAttributeRoutes();

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
