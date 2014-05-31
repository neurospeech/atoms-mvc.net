using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Atoms.Entity
{
    public static class EntityAtomsObjectContextHelper
    {



        public static IQueryable<T> WhereKey<T>(this IQueryable<T> q, object key) {
            Type type = typeof(T);
            PropertyInfo pinfo = type.GetEntityProperties(true).First().Property;

            ParameterExpression pe = Expression.Parameter(type);
            if (key.GetType() != pinfo.PropertyType)
            {
                key = Convert.ChangeType(key, pinfo.PropertyType);
            }
            Expression compare = Expression.Equal(Expression.Property(pe, pinfo), Expression.Constant(key));
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(compare, pe);
            return q.Where(lambda);
        }



        //public static Expression<Func<T, bool>> GetKeyExpression<T>(this ObjectContext context, object identity)
        //{
        //    Expression<Func<T, bool>> x = null;
        //    PropertyInfo p = typeof(T).GetProperties().First();
        //    ParameterExpression pe = Expression.Parameter(typeof(T));
        //    Expression ce = Expression.Equal(
        //        Expression.Property(pe, p),
        //        Expression.Convert(
        //            Expression.Constant(identity),
        //            p.PropertyType));
        //    x = Expression.Lambda<Func<T, bool>>(ce, pe);
        //    return x;
        //}

        private static ThreadSafeDictionary<Type, IEnumerable<AtomPropertyInfo>> keyList = new ThreadSafeDictionary<Type, IEnumerable<AtomPropertyInfo>>();
        private static ThreadSafeDictionary<Type, IEnumerable<AtomPropertyInfo>> allList = new ThreadSafeDictionary<Type, IEnumerable<AtomPropertyInfo>>();


        public static IEnumerable<AtomPropertyInfo> GetEntityProperties(this Type type, bool keyOnly = false)
        {
            ThreadSafeDictionary<Type, IEnumerable<AtomPropertyInfo>> cache = keyOnly ? keyList : allList;

            IEnumerable<AtomPropertyInfo> result = null;

            if (cache.TryGetValue(type, out result)) {
                return result;
            }

            List<AtomPropertyInfo> list = new List<AtomPropertyInfo>();

            foreach (PropertyInfo p in type.GetProperties())
            {
                EdmScalarPropertyAttribute a = p.GetCustomAttribute<EdmScalarPropertyAttribute>();
                if (a == null)
                    continue;
                if (keyOnly)
                {
                    if (a.EntityKeyProperty)
                    {
                        list.Add( new AtomPropertyInfo(type, p,true));
                    }
                }
                else
                {
                    list.Add(new AtomPropertyInfo(type, p, a.EntityKeyProperty));
                }

            }

            result = list;

            cache[type] = result;

            return result;
        }

        private static ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>> navProperties = new ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>>();

        public static IEnumerable<PropertyInfo> GetNavigationProperties(this Type type) {
            IEnumerable<PropertyInfo> result = null;
            if (navProperties.TryGetValue(type, out result))
                return result;
            List<PropertyInfo> list = new List<PropertyInfo>();
            Type relatedType = typeof(IRelatedEnd);
            Type enumType = typeof(System.Collections.IEnumerable);
            foreach (var item in type.GetProperties())
            {
                Type pType = item.PropertyType;
                if (relatedType.IsAssignableFrom(pType) && enumType.IsAssignableFrom(pType)) {
                    list.Add(item);
                }
            }
            result = list;
            navProperties[type] = result;
            return result;
        }

        public static T GetCustomAttribute<T>(this MemberInfo m)
            where T : class
        {
            object[] atrs = m.GetCustomAttributes(typeof(T), true);
            if (atrs == null || atrs.Length == 0)
                return null;
            return atrs[0] as T;
        }

        public static ObjectQuery<T> Where<T>(this ObjectQuery<T> query, Expression<Func<T, bool>> exp)
        {
            return Queryable.Where(query, exp) as ObjectQuery<T>;
        }




       
    }

}
