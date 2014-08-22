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
using System.Diagnostics;
using NeuroSpeech.Atoms.Mvc;

namespace NeuroSpeech.Atoms.Linq
{
    public class JsonExpression<T>
    {

        public IDictionary<string, object> Values { get; private set; }

        public BaseSecurityContext SecurityContext { get; private set; }

        public Expression<Func<T, bool>> Linq { get; private set; }

        public JsonExpression(string json, BaseSecurityContext context = null)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            Values = (IDictionary<string, object>)js.Deserialize(json, typeof(Dictionary<string, object>));
            this.SecurityContext = context;
        }

        public JsonExpression(IDictionary<string, object> values, BaseSecurityContext context = null)
        {
            this.Values = values;
            this.SecurityContext = context;
        }

        public Expression<Func<T, bool>> Parse()
        {
            ParameterExpression pe = Expression.Parameter(typeof(T), "x");
            var e = ParseLinq(pe, typeof(T), Values);
            if (e == null)
                return null;
            Linq = Expression.Lambda<Func<T, bool>>(e, pe);
            return Linq;
        }


        private Expression ParseLinq(Expression root, Type type, IDictionary<string, object> query, bool or = false)
        {

            Expression exr = null;
            foreach (KeyValuePair<string, object> key in query)
            {
                Expression er = null;
                switch (key.Key)
                {
                    case "$or":
                        er = ParseLinq(root, type, key.Value as IDictionary<string, object>, true);
                        break;
                    case "$not":
                        er = Expression.Not(ParseLinq(root, type, key.Value as IDictionary<string, object>));
                        break;
                    default:
                        er = ParseLinq(root, type, key.Key, key.Value);
                        break;
                }
                if (er == null)
                    continue;
                exr = exr == null ? er : (or ? Expression.Or(exr, er) : Expression.And(exr, er));
            }
            return exr;

        }

        private Expression ParseLinq(Expression root, Type type, string key, object value)
        {
            string[] tokens = key.Split(':', ' ').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

            string field = tokens[0];

            string operation = "==";
            if (tokens.Length > 1)
                operation = tokens[1];

            operation = operation.ToLower();

            foreach (var item in field.Split('.'))
            {
                PropertyInfo p = null;
                if (item.Equals("$id", StringComparison.OrdinalIgnoreCase))
                {
                    p = type.GetEntityProperties(true).FirstOrDefault().Property;
                }
                else
                {
                    p = type.GetProperty(item);
                }
                type = p.PropertyType;
                root = Expression.Property(root, p);
            }

            switch (operation)
            {
                case "any":
                    return ParseCollectionLinq("Any", root, type, value);
                case "all":
                    return ParseCollectionLinq("All", root, type, value);
                case "in":
                    return ParseCollectionLinq("Contains", Expression.Constant(CreateList(value,type)), type, root);
                case "!in":
                    return Expression.Not(ParseCollectionLinq("Contains", Expression.Constant(CreateList(value, type)), type, root));
                case "!any":
                    return Expression.Not(ParseCollectionLinq("Any", root, type, value));
                case "!all":
                    return Expression.Not(ParseCollectionLinq("All", root, type, value));
            }

            Expression ve = Expression.Constant(null);

            if (value != null)
            {
                Type valueType = value.GetType();
                Type vt = Nullable.GetUnderlyingType(type);
                if (valueType.IsArray || valueType == type)
                {
                    ve = Expression.Constant(value);
                }
                else
                {
                    if (vt != null) {
                        if (vt == typeof(DateTime) && value.GetType() == typeof(string))
                        {
                            ve = Expression.Constant( AtomJavaScriptSerializer.ToDateTime(value as string), type );
                        }
                        else
                        {
                            value = Convert.ChangeType(value, vt);
                            ve = Expression.Convert(Expression.Constant(value), type);
                        }
                    }
                    else
                    {
                        value = Convert.ChangeType(value, type);
                        ve = Expression.Constant(value);
                    }
                }
            }

            switch (operation)
            {
                case "==":
                    return Expression.Equal(root, ve);
                case "!=":
                    return Expression.NotEqual(root, ve);
                case ">":
                    return Expression.GreaterThan(root, ve);
                case ">=":
                    return Expression.GreaterThanOrEqual(root, ve);
                case "<":
                    return Expression.LessThan(root, ve);
                case "<=":
                    return Expression.LessThanOrEqual(root, ve);
                case "contains":
                    return Expression.Call(root, StringContainsMethod, ve);
                case "startswith":
                    return Expression.Call(root, StringStartsWithMethod, ve);
                case "endswith":
                    return Expression.Call(root, StringEndsWithMethod, ve);
                case "!contains":
                    return Expression.Not(Expression.Call(root, StringContainsMethod, ve));
                case "!startswith":
                    return Expression.Not(Expression.Call(root, StringStartsWithMethod, ve));
                case "!endswith":
                    return Expression.Not(Expression.Call(root, StringEndsWithMethod, ve));
                default:
                    break;
            }
            throw new ArgumentException(operation + " not supported");
        }

        private static MethodInfo StringContainsMethod = typeof(string).GetMethod("Contains", new Type[] { typeof(string) });
        private static MethodInfo StringStartsWithMethod = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
        private static MethodInfo StringEndsWithMethod = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });

        private object CreateList(object value, Type type)
        {
            Type lt = typeof(List<>).MakeGenericType(type);
            IList ic = Activator.CreateInstance(lt) as IList;
            foreach (var item in (ArrayList)value)
            {
                ic.Add( Convert.ChangeType(item,type));
            }
            return ic;
        }

        //private static Expression ParseInLinq(Expression root, Type type, object value)
        //{
        //    if (value == null)
        //        throw new ArgumentNullException("IN Parameter can not be null");


        //}

        private Expression ParseCollectionLinq(string operation, Expression root, Type type, object value)
        {



            //Dictionary<string, object> args = value as Dictionary<string, object>;

            var args = type.GetGenericArguments();

            if (args == null || args.Length == 0)
            {
                Type et = typeof(IEnumerable<>).MakeGenericType(type);
                return Expression.Call(typeof(Enumerable), operation, new Type[] { type }, root, (Expression)value);
            }

            type = args[0];

            if (SecurityContext != null)
            {
                root = this.ApplyFilter(root, type);
            }

            ParameterExpression pe = Expression.Parameter(type);

            Expression ae = value as Expression;
            if (ae == null)
            {
                ae = ParseLinq(pe, pe.Type, (IDictionary<string, object>)value);
            }

            var predicate = Expression.Lambda(typeof(Func<,>).MakeGenericType(type, typeof(bool)),
                ae,
                pe);

            return Expression.Call(typeof(Enumerable), operation, new Type[] { type }, root, predicate);

        }

        private Expression ApplyFilter(Expression root,Type type)
        {
            if (SecurityContext.IgnoreSecurity)
                return root;
            Expression exp = (Expression)BaseSecurityContext.GenericMethods.InvokeGeneric(SecurityContext, "GetReadRule", type, new object[] { null });
            if (exp == null)
                return root;
            return Expression.Call(typeof(Enumerable), "Where", new Type[] { type }, root, exp);
        }

        
    }
}
