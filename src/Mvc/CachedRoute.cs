using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Routing;

namespace NeuroSpeech.Atoms
{

    public class CachedRoute : HttpTaskAsyncHandler, IRouteHandler
    {

        private CachedRoute()
        {
            // only one per app..

        }

        private string Prefix { get; set; }

        public static string Version { get; private set; }

        private TimeSpan MaxAge { get; set; }

        //private static CachedRoute Instance;

        public static void Register(
            RouteCollection routes,
            TimeSpan? maxAge = null,
            string version = null)
        {
            CachedRoute sc = new CachedRoute();
            sc.MaxAge = maxAge == null ? TimeSpan.FromDays(30) : maxAge.Value;

            if (string.IsNullOrWhiteSpace(version))
            {
                version = System.Web.Configuration.WebConfigurationManager.AppSettings["Static-Content-Version"];
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }

            Version = version;

            var route = new Route("cached/{version}/{*name}", sc);
            route.Defaults = new RouteValueDictionary();
            route.Defaults["version"] = "1";
            routes.Add(route);
        }

        public override bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public static string CDNHost { get; set; }

        public static HtmlString CachedUrl(string p)
        {
            if (!p.StartsWith("/"))
                throw new InvalidOperationException("Please provide full path starting with /");
            string cdnPrefix = string.IsNullOrWhiteSpace(CDNHost) ? "" : ("//" + CDNHost);
            return new HtmlString(cdnPrefix + "/cached/" + Version + p);
        }

        //[Obsolete("Replace with CachedUrl",true)]
        //public static HtmlString Url(string p)
        //{
        //    throw new InvalidOperationException();
        //}

        public override async System.Threading.Tasks.Task ProcessRequestAsync(HttpContext context)
        {
            var Response = context.Response;
            Response.Cache.SetCacheability(HttpCacheability.Public);
            Response.Cache.SetMaxAge(MaxAge);
            Response.Cache.SetExpires(DateTime.Now.Add(MaxAge));

            string FilePath = context.Items["FilePath"] as string;

            var file = new FileInfo(context.Server.MapPath("/" + FilePath));
            if (!file.Exists)
            {
                throw new FileNotFoundException(file.FullName);
            }

            Response.ContentType = MimeMapping.GetMimeMapping(file.FullName);

            using (var fs = file.OpenRead())
            {
                await fs.CopyToAsync(Response.OutputStream);
            }
        }

        IHttpHandler IRouteHandler.GetHttpHandler(RequestContext requestContext)
        {
            //FilePath = requestContext.RouteData.GetRequiredString("name");
            requestContext.HttpContext.Items["FilePath"] = requestContext.RouteData.GetRequiredString("name");
            return (IHttpHandler)this;
        }
    }

}