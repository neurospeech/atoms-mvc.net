using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Web.Mvc;
using System.ComponentModel;

namespace NeuroSpeech.Atoms.Mvc
{
	public class AtomDictionary : 
		DynamicObject , 
		IJavaScriptSerializable, 
		IJavaScriptDeserializable
	{

        //public static AtomDictionary FromObject(object obj) {
        //    AtomDictionary a = new AtomDictionary();

        //    foreach (var item in TypeDescriptor.GetProperties(obj))
        //    {
        //        object val = item.GetValue(obj);
        //        if (val == null)
        //            continue;

        //        string sval = val as string;
        //        if (sval != null) {
        //            a.InternalDictionary[item.Name] = val;
        //            continue;
        //        }

        //        System.Collections.IEnumerable en = val as System.Collections.IEnumerable;
        //        if (en != null) {
        //            List<AtomDictionary> list = new List<AtomDictionary>();
        //            foreach (var child in en)
        //            {
        //                list.Add(FromObject(child));
        //            }
        //            a.InternalDictionary[item.Name] = list;
        //            continue;
        //        }

        //        Type valueType = val.GetType();
        //        if (valueType.IsValueType) {
        //            a.InternalDictionary[item.Name] = val;
        //            continue;
        //        }

        //        a.InternalDictionary[item.Name] = FromObject(val);

        //    }

        //    return a;
        //}

		public AtomDictionary()
		{

		}


		private Dictionary<string, object> dictionary = new Dictionary<string, object>();

		public int Count {
			get {
				return dictionary.Count;
			}
		}

		public Dictionary<string, object> InternalDictionary {
			get {
				return dictionary;
			}
		}

        public T GetValue<T>(string name, T def = default(T)) {
            object value = null;
            if (dictionary.TryGetValue(name, out value))
                return (T)Convert.ChangeType(value, typeof(T));
            return def;
        }

		#region public override bool  TryGetMember(GetMemberBinder binder, out object result)
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			string name = binder.Name;
			return dictionary.TryGetValue(name, out result);
		}
		#endregion


		#region public override bool  TrySetMember(SetMemberBinder binder, object value)
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			dictionary[binder.Name] = value;
			return true;
		}
		#endregion


		string IJavaScriptSerializable.Serialize(IJavaScriptSerializer js)
		{
            if (dictionary.Count == 0)
                return "{}";
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				sb.Append("\"" + item.Key + "\":");
				sb.Append(js.Serialize(item.Value));
				sb.Append(",");
			}
            sb[sb.Length - 1] = '}';
			return sb.ToString();
		}

		void IJavaScriptDeserializable.Deserialize(Dictionary<string, object> values)
		{
			foreach (var item in values)
			{
				object value = DeserializeValue(item.Value);
				dictionary[item.Key] = value;
			}
		}

		private object DeserializeValue(object value)
		{
			if (value == null)
				return null;

			string val = value as string;
			if (val != null)
			{
                //if (val.StartsWith("/DateISO"))
                //{
                //    int i = val.IndexOf('(');
                //    val = val.Substring(i + 1);
                //    i = val.LastIndexOf(')');
                //    val = val.Substring(0, i);
                //    return DateTime.Parse(val, null, System.Globalization.DateTimeStyles.RoundtripKind);
                //}
				if(val.StartsWith("/Date"))
					return AtomJavaScriptSerializer.ToDateTime(val);
				return val;
			}

			if (value is Dictionary<string, object>)
			{
				IJavaScriptDeserializable d = new AtomDictionary();
				d.Deserialize(value as Dictionary<string, object>);
				return d;
			}

			if (value is System.Collections.IEnumerable)
			{
				List<object> items = new List<object>();
				foreach (var item in (System.Collections.IEnumerable)value)
				{
					items.Add(DeserializeValue(item));
				}

				return items;
			}

			return value;
		}
	}
}
