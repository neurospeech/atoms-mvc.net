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
    public class EntityAccessException : Exception
    {
        //public System.Linq.Expressions.Expression<Func<T, bool>> Rule { get; private set; }

        public Type Type { get; private set; }


        public EntityAccessException(System.Type type, string message, string rule)
            : base(message)
        {
            this.Type = type;
            this.Source = rule;
        }
    }
}
