using NeuroSpeech.Atoms;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace System.Web.Mvc {

    /// <summary>
    /// 
    /// </summary>
    public class RegisterResourceAttribute : Attribute, IActionFilter
    {

        private IEnumerable<HtmlResource> Resources;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resources"></param>
        public RegisterResourceAttribute(params HtmlResource[] resources)
        {
            Resources = resources;
        }

        void IActionFilter.OnActionExecuted(ActionExecutedContext filterContext)
        {
            
        }

        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.Controller;
            foreach (var item in Resources)
            {
                controller.Register(item);
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public static class HtmlResourcesHelper {

        /// <summary>
        /// Register resource to be rendered on this page
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="resource"></param>
        public static void Register(this HtmlHelper helper, HtmlResource resource)
        {
            Register(helper.ViewContext.HttpContext, resource);
        }

        /// <summary>
        /// Register resource to be rendered on this page
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="resource"></param>
        public static void Register(this ControllerBase controller, HtmlResource resource)
        {
            Register(controller.ControllerContext.HttpContext, resource);
        }

        private static void Register(this HttpContextBase context, HtmlResource resource) {
            var rs = context.Items["HtmlResources"] as List<HtmlResource>;
            if (rs == null)
            {
                rs = new List<HtmlResource>();
                context.Items["HtmlResources"] = rs;
            }

            if (rs.Contains(resource))
                return;

            rs.Add(resource);
        }


        /// <summary>
        /// Render all registered resources, this must be used only inside a layout page or on the page without layout
        /// </summary>
        /// <param name="helper"></param>
        public static HelperResult RenderResources<T>(this HtmlHelper<T> helper) {
            return RenderResources((HtmlHelper)helper);
        }

        /// <summary>
        /// Render all registered resources, this must be used only inside a layout page or on the page without layout
        /// </summary>
        /// <param name="helper"></param>
        public static HelperResult RenderResources(this HtmlHelper helper)
        {

            return new HelperResult(sw =>
            {

                var rs = helper.ViewContext.HttpContext.Items["HtmlResources"] as List<HtmlResource>;
                if (rs == null)
                {
                    return;
                }

                List<HtmlResource> result = new List<HtmlResource>();
                Build(rs, result);

                StringBuilder sb = new StringBuilder();

                foreach (var item in result)
                {
                    item.Render(sw);
                }

                // remove all resources once rendered...
                rs.Clear();

            });
        }

        private static void Build(List<HtmlResource> src, List<HtmlResource> dest)
        {
            foreach (var item in src)
            {
                Build(item.Dependencies, dest);

                if (dest.Contains(item))
                    continue;

                dest.Add(item);
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class HtmlResource {

        /// <summary>
        /// 
        /// </summary>
        public static bool Cached
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool UseCDN { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static string CDNHost { get; set; }

        private static List<HtmlResource> registeredResources = new List<HtmlResource>();

        private static T Create<T>(string path, string cdnPath, params HtmlResource[] dependencies)
            where T:HtmlResource
        {
            lock (registeredResources) {
                if (registeredResources.Any( x=> string.Equals(x.Path, path, StringComparison.CurrentCultureIgnoreCase))) {
                    throw new InvalidOperationException("Resource " + path + " is already registered");
                }
                var rs = Activator.CreateInstance<T>();
                rs.Path = path;
                rs.ResourcePath = rs.Path;
                rs.CDNPath = cdnPath;

                if (Cached)
                {
                    if (string.IsNullOrWhiteSpace(cdnPath))
                    {
                        rs.ResourcePath = CachedRoute.CachedUrl(rs.ResourcePath).ToHtmlString();
                    }
                }
                if (UseCDN)
                {
                    if (!string.IsNullOrWhiteSpace(cdnPath))
                    {
                        rs.ResourcePath = cdnPath;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(CDNHost))
                        {
                            rs.ResourcePath = "//" + CDNHost + rs.ResourcePath;
                        }
                    }
                }
                if (dependencies != null && dependencies.Length > 0) {
                    rs.Dependencies = new List<HtmlResource>();
                    rs.Dependencies.AddRange(dependencies);
                }
                registeredResources.Add(rs);
                return rs;
            }

        }

        /// <summary>
        /// Creates Global Script Resource
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource RegisterGlobalScript(string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlScriptResource>(path, null, dependencies);
        }

        /// <summary>
        /// Creates Global Stylesheet Resource
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cdnPath"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource RegisterGlobalStyleSheet(string path, string cdnPath, params HtmlResource[] dependencies)
        {
            return Create<HtmlStyleSheetResource>(path, cdnPath, dependencies);
        }

        /// <summary>
        /// Creates Global Script Resource
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cdnPath"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource RegisterGlobalScript(string path, string cdnPath, params HtmlResource[] dependencies)
        {
            return Create<HtmlScriptResource>(path, cdnPath, dependencies);
        }

        /// <summary>
        /// Creates Global Stylesheet Resource
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static HtmlResource RegisterGlobalStyleSheet(string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlStyleSheetResource>(path, null, dependencies);
        }
        /// <summary>
        /// Creates inline Script Resource, that will be rendered in the Header
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static HtmlResource RegisterPageScript(string code) {
            var s = new HtmlScriptResource();
            s.Code = code;
            return s;
        }

        /// <summary>
        /// Creates JavaScript Variable with Provided name for Model
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HtmlResource RegisterPageScriptModel(string name, object model) {
            var s = new HtmlScriptResource();
            AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null);
            s.Code = "var " + name + " = " + js.Serialize(model) + ";";
            return s;
        }

        /// <summary>
        /// Creates inline Stylesheet Resource, that will be rendered in Header
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static HtmlResource RegisterPageStyle(string code)
        {
            var s = new HtmlStyleSheetResource();
            s.Code = code;
            return s;
        }

        /// <summary>
        /// 
        /// </summary>
        internal protected HtmlResource()
        {
        }

        internal string Path { get; set; }

        internal string CDNPath { get; set; }

        internal string Code { get; set; }

        internal List<HtmlResource> Dependencies { get;  set; }

        string RenderedCache { get; set; }

        internal abstract void Render(TextWriter sw);

        internal string ResourcePath { get; set; }
        
    }

    internal class HtmlScriptResource : HtmlResource {

        internal override void Render(TextWriter sw)
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                sw.WriteLine("<script src='{0}' type='text/javascript'></script>", ResourcePath);
                return;
            }
            sw.WriteLine("<script type='text/javascript'>\r\n\t{0}\r\n</script>\r\n", Code);
        }
    }

    internal class HtmlStyleSheetResource : HtmlResource {
        internal override void Render(TextWriter sw)
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                sw.WriteLine("<link rel='stylesheet' href='{0}'/>", ResourcePath);
                return;
            }
            sw.WriteLine("<style>\r\n{0}\r\n</style>\r\n", Code);
        }
    }



}
