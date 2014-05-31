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
using NeuroSpeech.Atoms.Linq;
using NeuroSpeech.Atoms;
using System.Linq.Expressions;
using System.Data.Entity.Core.Objects.DataClasses;

namespace System.Web.Mvc
{
    public class AtomJavaScriptSerializer : IJavaScriptSerializer {


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
            if (dt.StartsWith("-"))
            {

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

    		private JavaScriptSerializer JS = new JavaScriptSerializer();

    		//List<object> tree = null;
    		bool json = true;

            private BaseSecurityContext SecurityContext = null;

            public AtomJavaScriptSerializer(BaseSecurityContext bsc, bool json = true)
    		{
    			this.json = json;
    			//tree = new List<object>();
                this.SecurityContext = bsc;
    		}

    		public string Serialize(object obj)
    		{
    			if (obj == null)
    				return "null";
    			if (obj is string)
    			{
    				return QuotedString((string)obj);
    			}
    			if (obj is DateTime)
    			{
    				DateTime val = (DateTime)obj;
    				return ToJsonDateTime(val, json);
    			}
    			if (obj is bool)
    			{
    				return ((bool)obj) ? "true" : "false";
    			}
    			if (obj is Guid)
    			{
    				return QuotedString(((Guid)obj).ToString());
    			}
    			if (obj is IJavaScriptSerializable)
    			{
    				return ((IJavaScriptSerializable)obj).Serialize(this);
    			}

    			Type objType = obj.GetType();
    			if (objType.IsValueType)
    			{
    				string val = obj.ToString();
    				if (string.IsNullOrWhiteSpace(val))
    					return "null";
    				return val;
    			}


    			IEnumerable en = obj as IEnumerable;
    			if (en != null)
    			{
    				List<string> items = new List<string>();
    				foreach (object item in en)
    				{
    					items.Add(this.Serialize( item));
    				}
    				return "[" + string.Join(",", items) + "]";
    			}

                //if (!(obj is AtomEntity))
                //    return JS.Serialize(obj);

    			StringBuilder sb = new StringBuilder();
    			Type et = typeof(EntityObject);

                IEnumerable plist = TypeDescriptor.GetProperties(obj);

                if (obj is IEntityWrapper && SecurityContext != null) {
                    plist = (SecurityContext[obj.GetType().BaseType]).GetProperties(obj);
                }

    			foreach (PropertyDescriptor pd in plist)
    			{
                    if (pd.Attributes.OfType<XmlIgnoreAttribute>().Any())
                        continue;
                    if (pd.Attributes.OfType<ScriptIgnoreAttribute>().Any())
                        continue;

                    // ignore entity object properties like EntityKey EntityState etc..
                    if (pd.PropertyType.DeclaringType == et)
                        continue;

    				object value = GenericMethods.GetProperty(obj,pd);
                    //object value = pd.GetValue(obj);
                    //if (value != null && tree.Contains(value))
                    //    continue;
    				if (json)
    				{
    					sb.Append('"');
    					sb.Append(pd.Name);
    					sb.Append('"');
    				}
    				else
    				{
    					sb.Append(pd.Name);
    				}
    				sb.Append(":");
    				string jv = this.Serialize(value);
    				sb.Append(jv);
    				sb.Append(",");
    			}
    			if (sb.Length > 0)
    				sb = sb.Remove(sb.Length - 1, 1);
    			return "{" + sb.ToString() + "}";
    		}

    		private string QuotedString(string p)
    		{
    			return JS.Serialize(p);
    		}

    	}
}
