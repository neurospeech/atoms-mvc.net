using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc
{
    public static class AtomType
    {

        private static ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>> _properties = new ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>>();


        public static IEnumerable<PropertyInfo> GetCachedProperties(this Type type)
        {
            return _properties.GetOrAdd(type, t =>
            {
                return t.GetProperties();
            });
        }

        public static PropertyInfo GetCachedProperty(this Type type, string name, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return type.GetCachedProperties().FirstOrDefault(x => x.Name.Equals(name, comparison));
        }

        private static ThreadSafeDictionary<Type, ConstructorInfo> _constructors = new ThreadSafeDictionary<Type, ConstructorInfo>();

        public static ConstructorInfo GetCachedConstructor(this Type type)
        {
            return _constructors.GetOrAdd(type, t =>
            {
                return t.GetConstructors().Where(x => x.GetParameters() != null && (
                    x.GetParameters().Length == 0
                    || x.GetParameters().All(p => p.HasDefaultValue)
                    )).FirstOrDefault();
            });
        }


        /// <summary>
        /// Creates new instance of property type only if there is public constructor available
        /// </summary>
        /// <param name="p"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static object GetOrCreatePropertyValue(this PropertyInfo p, object target)
        {
            object r = p.GetValue(target);
            if (r == null)
            {
                // check if parameter less public constructor is available
                ConstructorInfo c = p.PropertyType.GetCachedConstructor();
                if (c != null)
                {
                    r = Activator.CreateInstance(p.PropertyType);
                    p.SetValue(target, r);
                }
            }
            return r;
        }

    }
}
