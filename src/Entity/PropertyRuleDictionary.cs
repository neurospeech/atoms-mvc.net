using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Atoms.Entity
{
    public class PropertyRuleDictionary
    {


        private ThreadSafeDictionary<string, EntityPropertyRules> Rules = new ThreadSafeDictionary<string, EntityPropertyRules>();

        public EntityPropertyRules this[Type type]
        {
            get
            {
                EntityPropertyRules list = null;
                lock (this)
                {
                    if (!Rules.TryGetValue(type.FullName, out list))
                    {
                        list = new EntityPropertyRules(type.Name, type.FullName);
                        // load only primary key rules...
                        foreach (var item in type.GetEntityProperties(true))
                        {
                            // only send...
                            list[item.Name] = SerializeMode.Read;
                        }

                        Rules[type.FullName] = list;
                    }
                }
                return list;
            }
        }

        public void Set<T>(string property, SerializeMode mode)
        {
            EntityPropertyRules modes = this[typeof(T)];
            modes[property] = mode;
        }

        public void Set<T>(Expression<Func<T, object>> exp, SerializeMode mode)
        {
            MemberExpression body;
            UnaryExpression u = exp.Body as UnaryExpression;
            if (u != null)
            {
                body = u.Operand as MemberExpression;
            }
            else
            {
                body = exp.Body as MemberExpression;
            }
            Set<T>(body.Member.Name, mode);
        }

        public void AllowSendReceive<T>(Expression<Func<T, object>> exp)
        {
            Set<T>(exp, SerializeMode.ReadWrite);
        }

        /*public void Apply(AtomEntity entity)
        {
            if (entity == null)
                return;
            Dictionary<string, SerializeMode> modes = this[entity.GetType()];
            entity.NetworkContext.Copy(modes);
        }*/

    }
}
