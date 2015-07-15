using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc.Mvc
{
    public class DictionaryModelBinder
    {

        private static ThreadSafeDictionary<Type, Func<object, object>> Binders = new ThreadSafeDictionary<Type, Func<object, object>>();


        public static void Bind(object model, IDictionary<string, object> values)
        {
            Type type = model.GetType();
            foreach (var item in values)
            {
                PropertyInfo p = type.GetCachedProperty(item.Key);
                if (p == null)
                    continue;
                if (!p.CanWrite)
                    continue;
                object val = item.Value;
                if (val != null)
                {
                    Type pt = p.PropertyType;

                    if (pt.IsGenericType)
                    {
                        if (pt.GetGenericTypeDefinition() == typeof(System.Nullable<>))
                        {
                            pt = pt.GetGenericArguments()[0];
                        }
                    }
                    if (pt == typeof(Guid))
                    {
                        val = Guid.Parse((string)val);
                    }
                    if (val is IDictionary<string, object>)
                    {
                        var src = p.GetOrCreatePropertyValue(model);
                        if (src != null)
                        {
                            Bind(src, (IDictionary<string, object>)val);
                        }
                        continue;
                    }
                    else if (val is System.Collections.IList)
                    {

                        Type objectType = p.PropertyType;
                        if (p.PropertyType.IsArray)
                        {
                            objectType = p.PropertyType.GetElementType();
                        }
                        else
                        {
                            objectType = p.PropertyType.GetGenericArguments()[0];
                        }

                        var src = p.GetOrCreatePropertyValue(model);
                        if (src != null)
                        {
                            var iList = src as System.Collections.IList;
                            if (iList != null)
                            {
                                foreach (object childItem in (System.Collections.IList)val)
                                {
                                    //iList.Add(childItem);
                                    var child = Activator.CreateInstance(objectType);
                                    iList.Add(child);
                                    Bind(child, childItem as IDictionary<string, object>);
                                }
                            }
                        }
                        continue;
                    }
                    else
                    {
                        val = Convert.ChangeType(val, pt);
                    }
                }

                object oldValue = p.GetValue(model, null);

                if (oldValue == val)
                    continue;
                if (oldValue != null && val != null && val.Equals(oldValue))
                    continue;

                p.SetValue(model, val, null);
            }

        }



    }
}
