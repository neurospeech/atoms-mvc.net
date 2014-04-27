using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using NeuroSpeech.Atoms.Mvc;
using NeuroSpeech.Atoms.Entity;

namespace NeuroSpeech.Atoms.Mvc
{
	//public static class AtomJavaScriptSerializer {
	//	public static string Serialize(object obj, bool json) {
	//		return System.Web.Mvc.WebAtomsMvcHelper._ToJavaScript(obj, json);
	//	}
	//}
}

namespace System.Web.Mvc
{

	

	public static class WebAtomsMvcHelper {

		public static HtmlString ToJavaScriptTag(this object obj, string varName) {
			string value = "<script type='text/javascript'>\r\n\tvar {0}={1};\r\n</script>\r\n";
			AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null,false);
			value = string.Format(value, varName, js.Serialize(obj));
			return new HtmlString(value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="format">Format should have placeholder {0} where JavaScript Object Literal will be placed in</param>
		/// <returns></returns>
		public static HtmlString ToJavaScript(this object obj, string format) {
			AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null,false);
			string value = string.Format(format, js.Serialize(obj));
			return new HtmlString(value);
		}

		/// <summary>
		/// Create JavaScript Object Literal Notation
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static HtmlString ToJavaScript(this object obj)
		{
			AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null,false);
			string value = js.Serialize(obj);
			return new HtmlString(value);
		}

		private static string QuotedString(string val)
		{
			JavaScriptSerializer js = new JavaScriptSerializer();
			return js.Serialize(val);
		}


		public static DateTime ToDateTime(string dt)
		{

            if (dt.StartsWith("/DateISO"))
            {
                int i = dt.IndexOf('(');
                dt = dt.Substring(i + 1);
                i = dt.LastIndexOf(')');
                dt = dt.Substring(0, i);
                var d = DateTime.Parse(dt, null, System.Globalization.DateTimeStyles.RoundtripKind);
                return d;
            }
			dt = dt.Substring(6);
			dt = dt.Substring(0, dt.Length - 2);
            if (dt.StartsWith("-")) { 
            
            }
			int z = dt.LastIndexOfAny(new[] { '+', '-' });
			if (z > 0)
				dt = dt.Substring(0, dt.Length - z + 1);
			//long ticks = long.Parse(dt) * 10000L + DatetimeMinTimeTicks;
			//return new DateTime(ticks, DateTimeKind.Utc);

            var date = new DateTime(1970, 1, 1);
            date = date.AddMilliseconds(long.Parse(dt));
            return date;
		}

		public static string ToJsonDateTime(DateTime val, bool json)
		{
			StringBuilder sb = new StringBuilder();
			if (json)
			{
				sb.Append("\"\\/Date(");
				sb.Append((val.Ticks - DatetimeMinTimeTicks) / 10000L);
				sb.Append(")\\/\"");
			}
			else
			{
				sb.Append("new Date(");
				sb.Append((val.Ticks - DatetimeMinTimeTicks) / 10000L);
				sb.Append(")");
			}
			return sb.ToString();
		}

		public static readonly long DatetimeMinTimeTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;



    }

    public interface IJavaScriptSerializer {
        string Serialize(object obj);
    }

	public interface IJavaScriptSerializable {
		string Serialize(IJavaScriptSerializer js);
	}

	public interface IJavaScriptDeserializable {
		void Deserialize(Dictionary<string, object> values);
	}
}
