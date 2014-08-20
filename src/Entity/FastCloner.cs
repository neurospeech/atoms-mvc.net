using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{
    public class FastCloner
    {

        static ThreadSafeDictionary<Type, Action<object, object>> Cache = new ThreadSafeDictionary<Type, Action<object, object>>();

        internal static void Merge(object storeEntity, object entity)
        {
            Type t = entity.GetType();
            Action<object, object> cloner = Cache.GetOrAdd(t, CreateCloner);
            cloner(storeEntity, entity);
        }

        internal static Action<object, object> CreateCloner(Type t)
        {

            List<Expression> statements = new List<Expression>();

            ParameterExpression pLeft = Expression.Parameter(typeof(object));
            ParameterExpression pRight = Expression.Parameter(typeof(object));

            Expression left = Expression.Variable(t, "left");
            Expression right = Expression.Variable(t, "right");

            statements.Add(Expression.Assign(left, Expression.TypeAs(pLeft, t)));
            statements.Add(Expression.Assign(right, Expression.TypeAs(pRight, t)));

            foreach (var prop in t.GetProperties())
            {
                statements.Add(Expression.Assign(Expression.Property(left, prop), Expression.Property(right, prop)));
            }

            BlockExpression block = Expression.Block(statements.ToArray());

            Expression<Action<object, object>> l = Expression.Lambda<Action<object, object>>(block, pLeft, pRight);

            return l.Compile();
        }
    }
}
