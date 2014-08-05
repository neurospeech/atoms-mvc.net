using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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


        public static IQueryable<T> OrderBy<T>(this IQueryable<T> q, string sortBy) {
            string[] tokens = sortBy.Split();
            if (tokens.Length == 0)
                return q;
            bool desc = false;
            if (tokens.Length == 2)
            {
                if (string.Compare(tokens[1], "desc", true) == 0)
                {
                    desc = true;
                }
            }

            ParameterExpression pe = Expression.Parameter(typeof(T));

            tokens = tokens[0].Split('.');

            Expression m = pe;
            foreach (var item in tokens)
            {
                m = Expression.Property(m, item);
            }

            MemberExpression me = m as MemberExpression;

            var p = me.Member as System.Reflection.PropertyInfo;

            var method = typeof(EntityAtomsObjectContextHelper).GetMethod("InvokeOrderBy").MakeGenericMethod(typeof(T),p.PropertyType);
            return (IQueryable<T>)method.Invoke(q, new object[] {q, me, pe, desc, true });
        }

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> q,string sortBy)
        {
            string[] tokens = sortBy.Split();
            if (tokens.Length == 0)
                return q;
            bool desc = false;
            if (tokens.Length == 2)
            {
                if (string.Compare(tokens[1], "desc", true) == 0)
                {
                    desc = true;
                }
            }

            ParameterExpression pe = Expression.Parameter(typeof(T));
            MemberExpression me = Expression.Property(pe, tokens[0]);

            var p = me.Member as System.Reflection.PropertyInfo;

            var method = typeof(EntityAtomsObjectContextHelper).GetMethod("InvokeOrderBy").MakeGenericMethod(typeof(T),p.PropertyType);
            return (IOrderedQueryable<T>)method.Invoke(q, new object[] {q, me, pe, desc, false });
        }


        public static IQueryable<T> InvokeOrderBy<T,TX>(IQueryable<T> q, Expression ex, ParameterExpression pe, bool desc, bool first)
        {
            Expression<Func<T, TX>> l = Expression.Lambda<Func<T, TX>>(ex, pe);

            if (first)
            {
                if (desc)
                    return q.OrderByDescending(l);
                return q.OrderBy(l);
            }
            if (desc)
            {
                return ((IOrderedQueryable<T>)q).ThenByDescending(l);
            }
            return ((IOrderedQueryable<T>)q).ThenBy(l);
        }


        public static IQueryable<T> WhereKey<T>(this IQueryable<T> q, object key) {
            Type type = typeof(T);
            PropertyInfo pinfo = type.GetEntityProperties(true).First().Property;

            ParameterExpression pe = Expression.Parameter(type);
            if (key.GetType() != pinfo.PropertyType)
            {
                try
                {
                    key = Convert.ChangeType(key, pinfo.PropertyType);
                }
                catch (Exception ex){
                    throw new InvalidOperationException("Conversion failed from " + key.GetType().FullName + " to " + pinfo.PropertyType.FullName, ex);
                }
            }
            Expression compare = Expression.Equal(Expression.Property(pe, pinfo), Expression.Constant(key));
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(compare, pe);
            return q.Where(lambda);
        }


        public static IQueryable<T> WhereCopy<T>(this IQueryable<T> q, T copy) 
        {
            Type entityType = typeof(T);

            ParameterExpression pe = Expression.Parameter(entityType);
            Expression c = null;

            foreach (var item in entityType.GetEntityProperties(true))
            {
                object src = item.GetValue(copy);
                src = Convert.ChangeType(src,item.PropertyType);
                Expression e = Expression.Equal(Expression.Property(pe, item.Property), Expression.Constant(src));
                c = c == null ? e : Expression.And(c, e);
            }

            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(c, pe);
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
                EntityPropertyAttribute a = p.GetCustomAttribute<EntityPropertyAttribute>();
                if (a == null)
                    continue;
                if (keyOnly)
                {
                    if (a.IsKey)
                    {
                        list.Add( new AtomPropertyInfo(type, p,true));
                    }
                }
                else
                {
                    list.Add(new AtomPropertyInfo(type, p, false));
                }

            }

            result = list;

            cache[type] = result;

            return result;
        }

        //private static ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>> navProperties = new ThreadSafeDictionary<Type, IEnumerable<PropertyInfo>>();

        //public static IEnumerable<PropertyInfo> GetNavigationProperties(this Type type) {
        //    IEnumerable<PropertyInfo> result = null;
        //    if (navProperties.TryGetValue(type, out result))
        //        return result;
        //    List<PropertyInfo> list = new List<PropertyInfo>();
        //    Type relatedType = typeof(IRelatedEnd);
        //    Type enumType = typeof(System.Collections.IEnumerable);
        //    foreach (var item in type.GetProperties())
        //    {
        //        Type pType = item.PropertyType;
        //        if (relatedType.IsAssignableFrom(pType) && enumType.IsAssignableFrom(pType)) {
        //            list.Add(item);
        //        }
        //    }
        //    result = list;
        //    navProperties[type] = result;
        //    return result;
        //}

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
