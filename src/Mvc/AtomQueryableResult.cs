using NeuroSpeech.Atoms.Entity;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace NeuroSpeech.Atoms.Mvc.Mvc
{
    public class AtomQueryableResult<T> : ActionResult
            where T : class
    {
        public IQueryable<T> Query { get; private set; }

        public long Total { get; set; }

        public BaseSecurityContext SecurityContext { get; private set; }

        public ISecureRepository Repository { get; set; }

        public AtomQueryableResult(IQueryable<T> q, long count = 0)
        {
            Query = q;
            Total = count;
        }

        public AtomQueryableResult(IQueryable<T> q, ISecureRepository repository, long count = 0)
        {
            Query = q;
            Total = count;
            this.Repository = repository;
            if (repository != null)
            {
                this.SecurityContext = repository.SecurityContext;
            }
        }

        public static AtomQueryableResult<TX> From<TX>(IQueryable<TX> q)
            where TX : class
        {
            return new AtomQueryableResult<TX>(q);
        }

        public AtomQueryableResult<T> Where(Expression<Func<T, bool>> filter)
        {
            Query = Query.Where(filter);
            return this;
        }

        public AtomQueryableResult<T> Where(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return this;
            Query = Query.WhereJsonQuery(query, Repository);
            return this;
        }

        public AtomQueryableResult<T> WhereKey(object key)
        {
            Type type = typeof(T);
            PropertyInfo pinfo = type.GetEntityProperties(true).First().Property;

            ParameterExpression pe = Expression.Parameter(type);
            if (key.GetType() != pinfo.PropertyType)
            {
                key = Convert.ChangeType(key, pinfo.PropertyType);
            }
            Expression compare = Expression.Equal(Expression.Property(pe, pinfo), Expression.Constant(key));
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(compare, pe);
            return Where(lambda);

        }

        public AtomQueryableResult<TR> SelectMany<TR>(Expression<Func<T, IEnumerable<TR>>> filter)
            where TR : class
        {
            return new AtomQueryableResult<TR>(Query.SelectMany(filter), Repository);
        }


        //public AtomQueryableResult<T> Include(string property)
        //{
        //    SerializationContext.Current.Include<T>(property);
        //    Query = ((ObjectQuery<T>)(Query)).Include(property);
        //    return this;
        //}

        public AtomQueryableResult<T> OrderBy<TKey>(Expression<Func<T, TKey>> sort)
        {
            Query = Query.OrderBy(sort);
            return this;
        }

        public AtomQueryableResult<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> sort)
        {
            Query = Query.OrderByDescending(sort);
            return this;
        }

        public AtomQueryableResult<T> ThenBy<TKey>(Expression<Func<T, TKey>> sort)
        {
            Query = ((IOrderedQueryable<T>)Query).ThenBy(sort);
            return this;
        }

        public AtomQueryableResult<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> sort)
        {
            Query = ((IOrderedQueryable<T>)Query).ThenByDescending(sort);
            return this;
        }

        public AtomQueryableResult<T> Page(int? start, int? count)
        {
            if (start == null || count == null)
                return this;
            if (start <= 0 && count <= -1)
                return this;
            if (start < 0)
                start = 0;
            Total = Query.Count();
            IOrderedQueryable<T> ot = Query as IOrderedQueryable<T>;
            if (ot == null)
                throw new InvalidOperationException("Paging requires Ordering first");
            Query = Query.Skip(start.GetValueOrDefault()).Take(count.Value);
            return this;
        }

        public AtomQueryableResult<T> Page(int start, int count)
        {
            if (start == 0 && count == -1)
                return this;
            Total = Query.Count();
            IOrderedQueryable<T> ot = Query as IOrderedQueryable<T>;
            if (ot == null)
                throw new InvalidOperationException("Paging requires Ordering first");
            Query = Query.Skip(start).Take(count);
            return this;
        }

        public AtomQueryableResult<object> Select<TResult>(Expression<Func<T, TResult>> selector)
            where TResult : class
        {
            //if (this.SecurityContext != null)
            //{
            //    ExpressionWalker w = new ExpressionWalker(this.SecurityContext);
            //    Expression<Func<T, object>> f = Expression.Lambda<Func<T, object>>(w.Visit(selector.Body), selector.Parameters);
            //    return new AtomQueryableResult<object>(Query.Select(f), this.SecurityContext, this.Total);
            //}

            return new AtomQueryableResult<object>(Query.Select<T, TResult>(selector), this.Repository, this.Total);
        }

        public AtomQueryableResult<object> Select(Dictionary<string, string> select)
        {
            return new AtomQueryableResult<object>((IQueryable<object>)Query.SelectDynamic(select), this.Repository, this.Total);
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(this.SecurityContext, true);
            if (Total > 0)
            {
                var obj = new { items = Query.ToList(), total = Total, merge = true };
                context.HttpContext.Response.Write(js.Serialize(obj));
            }
            else
            {
                var items = Query.ToList();
                context.HttpContext.Response.Write(js.Serialize(items));
            }
        }



        public void Skip(int? start)
        {
            if (start != null)
                Query = Query.Skip(start.Value);
        }

        public void Take(int? size)
        {
            if (size != null)
                Query = Query.Take(size.Value);
        }

        public AtomQueryableResult<T> Take(int size)
        {
            Query = Query.Take(size);
            return this;
        }

        public AtomQueryableResult<T> OrderBy(string sortBy)
        {
            string[] tokens = sortBy.Split();
            if (tokens.Length == 0)
                return this;
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

            var method = GetType().GetMethod("InvokeOrderBy").MakeGenericMethod(p.PropertyType);
            return (AtomQueryableResult<T>)method.Invoke(this, new object[] { me, pe, desc, true });
        }

        public AtomQueryableResult<T> ThenBy(string sortBy)
        {
            string[] tokens = sortBy.Split();
            if (tokens.Length == 0)
                return this;
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

            var method = GetType().GetMethod("InvokeOrderBy").MakeGenericMethod(p.PropertyType);
            return (AtomQueryableResult<T>)method.Invoke(this, new object[] { me, pe, desc, false });
        }


        public AtomQueryableResult<T> InvokeOrderBy<TX>(Expression ex, ParameterExpression pe, bool desc, bool first)
        {
            Expression<Func<T, TX>> l = Expression.Lambda<Func<T, TX>>(ex, pe);

            if (first)
            {
                if (desc)
                    return OrderByDescending(l);
                return OrderBy(l);
            }
            if (desc)
            {
                return ThenByDescending(l);
            }
            return ThenBy(l);
        }



        public string ToTraceString()
        {
            return ((ObjectQuery)Query).ToTraceString();
        }
    }
}
