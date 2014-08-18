using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System.Web.Mvc
{
    public static class JavaScriptTagHelper
    {
        public static HtmlString ToJavaScriptTag(this object obj, string varName)
        {
            string value = "<script type='text/javascript'>\r\n\tvar {0}={1};\r\n</script>\r\n";
            AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null, false);
            value = string.Format(value, varName, js.Serialize(obj));
            return new HtmlString(value);
        }
    }
}