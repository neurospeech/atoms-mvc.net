using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Transactions;
using NeuroSpeech.Atoms.Entity;
using System.Collections.Concurrent;

namespace NeuroSpeech.Atoms.Mvc
{

    /// <summary>
    /// GenericMethods dynamically compiles and caches generic methods.
    /// Because invoking generic methods with reflection requires more CPU time on validating and resolving parameters.
    /// </summary>
    public class GenericMethods
    {

        private static ThreadSafeDictionary<string, Func<object, object[], object>> CachedMethods = new ThreadSafeDictionary<string, Func<object, object[], object>>();

        public static object InvokeGeneric(object instance, string name, Type[] types, params object[] arguments) {
            Type instanceType = instance.GetType();

            string key = instanceType.FullName + ":" + name + ":" + string.Join(":", types.Select(t=>t.FullName));

            Func<object, object[], object> result = CachedMethods.GetOrAdd(key, a =>
            {
                return CreateMethod(instanceType, name, types);
            });

            return result(instance, arguments);
        }

        public static object InvokeGeneric(object instance, string name, Type type, params object[] arguments)
        {

            Type instanceType = instance.GetType();

            string key = instanceType.FullName + ":" + name + ":" + type.FullName;

            Func<object, object[], object> result = CachedMethods.GetOrAdd(key, a =>
            {
                return CreateMethod(instanceType, name, type);
            });

            return result(instance, arguments);
        }

        private static Func<object, object[], object> CreateMethod(Type instanceType, string name,params Type[] types)
        {


            ParameterExpression pe = Expression.Parameter(typeof(object));
            MethodInfo m = instanceType.GetMethods().FirstOrDefault(x=> x.IsGenericMethod && x.Name == name).MakeGenericMethod(types);
            ParameterExpression peArray = Expression.Parameter(typeof(object[]));
            List<Expression> ps = new List<Expression>();
            int i = 0;
            foreach (var pm in m.GetParameters())
            {
                Type pt = pm.ParameterType;
                Expression ce = (!pt.IsValueType) 
                    ? Expression.TypeAs(Expression.ArrayIndex(peArray, Expression.Constant(i)), pt)
                    : Expression.Convert(Expression.ArrayIndex(peArray, Expression.Constant(i)), pt);
                ps.Add(ce);
                i++;
            }

            MethodCallExpression me = Expression.Call(Expression.Convert(pe, instanceType), m, ps.ToArray());


            if (m.ReturnType == null || m.ReturnType == typeof(void))
            {

                var fx = Expression.Lambda<Action<object, object[]>>(me, pe, peArray).Compile();
                return (a,b)=>{
                    fx(a, b);
                    return null;
                };
                    
            }

            var le = Expression.Lambda<Func<object, object[], object>>(me, pe, peArray);

            return le.Compile();
        }


        private static ThreadSafeDictionary<string, Func<object, object>> CachedProperties = new ThreadSafeDictionary<string, Func<object, object>>();

        internal static object GetProperty(object obj, PropertyDescriptor pd)
        {
            if(obj==null)
                return null;
            Type objType = obj.GetType();
            string key =  objType.FullName + ":" + pd.Name;
            Func<object, object> f = CachedProperties.GetOrAdd(key, k => {
                ParameterExpression pe = Expression.Parameter(typeof(object), "x");
                Expression me = Expression.Property(Expression.TypeAs( pe, objType), objType.GetProperty(pd.Name));
                me = Expression.Convert(me, typeof(object));
                return Expression.Lambda<Func<object, object>>(me, pe).Compile();
            });

            return f(obj);
        }

        internal static object GetProperty(object obj, PropertyInfo p)
        {
            if (obj == null)
                return null;
            Type objType = obj.GetType();
            string key = objType.FullName + ":" + p.Name;
            Func<object, object> f = CachedProperties.GetOrAdd(key, k =>
            {
                ParameterExpression pe = Expression.Parameter(typeof(object), "x");
                Expression me = Expression.Property(Expression.TypeAs(pe, objType), p);
                me = Expression.Convert(me, typeof(object));
                return Expression.Lambda<Func<object, object>>(me, pe).Compile();
            });

            return f(obj);
        }

    }
}
