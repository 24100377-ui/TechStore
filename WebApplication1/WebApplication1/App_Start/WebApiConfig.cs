using System.Web.Http;
using System.Web.Http.Cors;

namespace WebApplication1
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Chỉ trả về JSON
            config.Formatters.XmlFormatter.SupportedMediaTypes.Clear();

            // CORS – cho phép mọi origin, mọi header, GET/POST/PUT/DELETE/OPTIONS
            // Trên production hãy thay "*" bằng domain cụ thể, ví dụ "https://yourfrontend.com"
            var cors = new EnableCorsAttribute(
                origins: "*",
                headers: "*",
                methods: "GET,POST,PUT,DELETE,OPTIONS");
            config.EnableCors(cors);

            // Attribute routing
            config.MapHttpAttributeRoutes();

            // Convention routing
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}