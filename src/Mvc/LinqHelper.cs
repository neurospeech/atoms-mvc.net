using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using NeuroSpeech.Atoms.Entity;
using NeuroSpeech.Atoms.Linq;

namespace System.Linq
{
    public static class LinqHelper
    {


        public static IQueryable<T> WhereJsonQuery<T>(this IQueryable<T> q, string query, BaseSecurityContext security = null)
        {
            JsonExpression<T> exp = new JsonExpression<T>(query, security);
            var e = exp.Parse();
            if (e == null)
                return q;
            return q.Where(e);
        }

        //public static IQueryable SelectDynamic(this IQueryable source, string select)
        //{
        //    JavaScriptSerializer js = new JavaScriptSerializer();
        //    var d =  (Dictionary<string,string>) js.Deserialize(select, typeof(Dictionary<string, string>));

        //    return SelectDynamic(source, (Dictionary<string, string>)d);
        //}


        public static IQueryable SelectDynamic(this IQueryable source, Dictionary<string, string> fieldNames)
        {
            Dictionary<string, PropertyInfo> sourceProperties = fieldNames.ToDictionary(name => name.Key, name => source.ElementType.GetDynamicProperty(string.IsNullOrWhiteSpace(name.Value) ? name.Key : name.Value));

            ParameterExpression sourceItem = Expression.Parameter(source.ElementType, "t");
            LinqFields fields = new LinqFields();

            foreach (var item in fieldNames.OrderBy(x=>x.Key))
	        {
                LinqField f = new LinqField{};
                f.Name = item.Key;
                f.PropertyPath = string.IsNullOrWhiteSpace(item.Value) ? item.Key : item.Value;
                f.Property = source.ElementType.GetDynamicProperty(f.PropertyPath);
                f.Type = f.Property.PropertyType;
                f.Expression = LinqRuntimeTypeBuilder.NestedProperty(sourceItem, source.ElementType, f.PropertyPath);
                fields.Add(f);
	        }

            Type dynamicType = LinqRuntimeTypeBuilder.GetDynamicType(fields);

            Expression selector = Expression.Lambda(Expression.MemberInit(Expression.New(dynamicType), 
                fields.Select( x=> Expression.Bind( dynamicType.GetField("_" + x.Name), x.Expression)   ).ToArray()),sourceItem);

            return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), "Select", new Type[] { source.ElementType, dynamicType },
                         Expression.Constant(source), selector));
        }

        //public static Expression<Func<T, bool>> ParseLinqExpression<T>(string query)
        //{
        //    JavaScriptSerializer js = new JavaScriptSerializer();
        //    var d = (Dictionary<string, object>)js.Deserialize(query, typeof(Dictionary<string, object>));
        //    ParameterExpression pe = Expression.Parameter(typeof(T), "x");
        //    var e = ParseLinq(pe, typeof(T), d);
        //    if (e == null)
        //        return null;
        //    return Expression.Lambda<Func<T, bool>>(e, pe);
        //}

        //public static Expression<Func<T, bool>> ParseLinqExpression<T>(Dictionary<string, object> query) {
        //    ParameterExpression pe = Expression.Parameter(typeof(T), "x");
        //    var e = ParseLinq(pe, typeof(T), query);
        //    if (e == null)
        //        return null;
        //    return Expression.Lambda<Func<T, bool>>(e, pe);
        //}




    }
}
