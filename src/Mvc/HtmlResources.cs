using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.Mvc {
    public static class HtmlResourcesHelper {

        /// <summary>
        /// Register resource to be rendered on this page
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="resource"></param>
        public static void Register(this HtmlHelper helper, HtmlResource resource)
        { 
            var rs = helper.ViewContext.HttpContext.Items["HtmlResources"] as List<HtmlResource>;
            if (rs == null) {
                rs = new List<HtmlResource>();
                helper.ViewContext.HttpContext.Items["HtmlResources"] = rs;
            }

            if (rs.Contains(resource))
                return;

            rs.Add(resource);

        }

        /// <summary>
        /// Render all registered resources, this must be used only inside a layout page or on the page without layout
        /// </summary>
        /// <param name="helper"></param>
        public static void RenderResources(this HtmlHelper helper)
        {
            var rs = helper.ViewContext.HttpContext.Items["HtmlResources"] as List<HtmlResource>;
            if (rs == null)
            {
                // nothing to render...
                return;
            }

            List<HtmlResource> result = new List<HtmlResource>();
            Build(rs,result);

            foreach (var item in result)
            {
                helper.Raw(item.ToString() + "\r\n");
            }

            // remove all resources once rendered...
            rs.Clear();
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
}

namespace NeuroSpeech.Atoms.Mvc
{

    public abstract class HtmlResource {

        private static Dictionary<string, HtmlResource> Resources = new Dictionary<string, HtmlResource>();

        private static T Create<T>(string name, string path, params HtmlResource[] dependencies)
            where T:HtmlResource
        {
            lock (Resources) {
                if (Resources.ContainsKey(name)) {
                    throw new InvalidOperationException("Resource with name " + name + " already exists");
                }
                var rs = Activator.CreateInstance<T>();
                rs.Name = name;
                rs.Path = path;
                rs.Dependencies = new List<HtmlResource>();
                if (dependencies != null && dependencies.Length > 0) {
                    rs.Dependencies.AddRange(dependencies);
                }
                Resources[name] = rs;
                return rs;
            }

        }

        public static HtmlResource CreateScript(string name, string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlScriptResource>(name, path, dependencies);
        }

        public static HtmlResource CreateStyleSheet(string name, string path, params HtmlResource[] dependencies)
        {
            return Create<HtmlStyleSheetResource>(name, path, dependencies);
        }


        protected HtmlResource()
        {
            Dependencies = new List<HtmlResource>();
        }

        /// <summary>
        /// Has to be a unique Name, probably same as NuGet Package
        /// </summary>
        internal string Name { get; set; }

        internal string Path { get; set; }

        internal List<HtmlResource> Dependencies { get;  set; }
    }

    internal class HtmlScriptResource : HtmlResource {
        public override string ToString()
        {
            return string.Format("<script src='stylesheet' href='{0}' type='text/javascript'></script>", Path);
        }
    }

    internal class HtmlStyleSheetResource : HtmlResource {
        public override string ToString()
        {
            return string.Format("<link rel='stylesheet' href='{0}'>",Path);
        }
    }



}
