using NeuroSpeech.Atoms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.Mvc
{
    public static class PageResources
    {

        public static IHtmlString RenderScript(this HtmlHelper html, string name, string min = ".min", bool cached = true) 
        {
            string path = name + min + ".js";

            if(cached){
                path = CachedRoute.CachedUrl(path).ToString();
            }

            return new HtmlString("<script src=\"" + path + "\" type=\"text/javascript\"></script>"); 
        }

        public static IHtmlString RenderStyle(this HtmlHelper html, string name, string min = ".min", bool cached = true)
        {
            string path = name + min + ".css";

            if (cached)
            {
                path = CachedRoute.CachedUrl(path).ToString();
            }

            return new HtmlString("<link href=\"" + path + "\" rel=\"stylesheet\"/>");
        }
    }
}
