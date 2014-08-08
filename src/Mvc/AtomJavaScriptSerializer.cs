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
using NeuroSpeech.Atoms.Mvc.Entity;

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

            private BaseSecurityContext SecurityContext;
            
            public AtomJavaScriptSerializer(BaseSecurityContext sc, bool json = true)
    		{
    			this.json = json;
                this.SecurityContext = sc;
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

                

                Type type = obj.GetType();

                IEnumerable<KeyValuePair<string,object>> propertyValues = null;

                IRepositoryObject iro = obj as IRepositoryObject;
                if (iro != null) {
                    type = iro.ObjectType;
                    if (SecurityContext != null) {
                        var security = SecurityContext[type];
                        propertyValues = GetPropertyValues(type, obj, security.PublicProperties);
                    }
                }


                if (propertyValues == null) {
                    propertyValues = GetPropertyValues(type, obj);
                }

    			foreach (var pd in propertyValues)
    			{

                    object value = pd.Value;
    				if (json)
    				{
    					sb.Append('"');
    					sb.Append(pd.Key);
    					sb.Append('"');
    				}
    				else
    				{
    					sb.Append(pd.Key);
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


            

            private IEnumerable<KeyValuePair<string, object>> GetPropertyValues(Type type, object obj, IEnumerable<string> filter = null)
            {
                IEnumerable<PropertyInfo> plist = type.GetProperties();
                if (filter != null) {
                    plist = plist.Where(x => filter.Contains(x.Name));
                }

                foreach (var item in plist)
                {
                    yield return new KeyValuePair<string,object>( item.Name, GenericMethods.GetProperty(obj,item) );
                }


            }

            static GenericMethods GenericMethods = new GenericMethods();

    		private string QuotedString(string p)
    		{
    			return JS.Serialize(p);
    		}

    	}
}
