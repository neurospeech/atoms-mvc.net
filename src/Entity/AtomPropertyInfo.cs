using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Atoms.Entity
{
    public class AtomPropertyInfo
    {

        private Func<object, object> GetHandler;
        private Action<object, object> SetHandler;

        public AtomPropertyInfo(Type type, PropertyInfo p, bool isKey)
        {
            Name = p.Name;
            this.IsKey = isKey;
            PropertyType = p.PropertyType;
            Property = p;

            List<FKPropertyAttribute> list = new List<FKPropertyAttribute>();
            Object[] attrs = p.GetCustomAttributes(typeof(FKPropertyAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
                foreach (FKPropertyAttribute at in attrs)
                {
                    list.Add(at);
                }
            }
            FKProperties = list.ToArray();

            

            ParameterExpression pe = Expression.Parameter(typeof(object));
            Expression me = Expression.Property(Expression.TypeAs(pe, type), p);

            GetHandler = Expression.Lambda<Func<object, object>>(Expression.TypeAs(me, typeof(object)), pe).Compile();

            ParameterExpression pe2 = Expression.Parameter(typeof(object));

            Expression value = pe2;
            if (PropertyType.IsValueType)
            {
                value = Expression.Convert(value, PropertyType);
            }
            else
            {
                value = Expression.TypeAs(value, PropertyType);
            }

            Expression assign = Expression.Assign(Expression.Property(Expression.TypeAs(pe, type), p), value);

            SetHandler = Expression.Lambda<Action<object, object>>(assign, pe, pe2).Compile();
        }

        public PropertyInfo Property { get; private set; }
        public string Name { get; private set; }
        public Type PropertyType { get; private set; }
        public bool IsKey { get; private set; }

        public IEnumerable<FKPropertyAttribute> FKProperties { get; private set; }

        public object GetValue(object target)
        {
            return GetHandler(target);
        }

        public void SetValue(object target, object value)
        {
            SetHandler(target, value);
        }
    }
}
