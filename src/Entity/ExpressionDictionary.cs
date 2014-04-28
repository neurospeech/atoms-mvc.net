using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Atoms.Entity
{
    public class ExpressionDictionary
    {

        Dictionary<Type, object> values = new Dictionary<Type, object>();

        public void Set<T>(Func<T, Expression<Func<T, bool>>> where)
        {
            values[typeof(T)] = where;
        }

        public void SetNoAccess<T>()
        {
            Set<T>(y =>
            {
                throw new InvalidOperationException();
            });
        }

        public bool EnableDefaultRule { get; set; }

        public Func<T, Expression<Func<T, bool>>> Get<T>()
        {
            Func<T, Expression<Func<T, bool>>> fx = null;
            object val = null;
            if (values.TryGetValue(typeof(T), out val))
            {
                fx = val as Func<T, Expression<Func<T, bool>>>;
            }
            if (fx == null && EnableDefaultRule)
                return y => x => true;
            return fx;
        }

    }
}
