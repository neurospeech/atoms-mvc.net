using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{
    public class EntityPropertyRulesCreator<TContext,T>
        where TContext:ISecureRepository
        where T:class
    {

        public EntityPropertyRules Rules { get; private set; }

        public Func<object, Expression<Func<T, bool>>> NotSupportedRule {
            get
            {
                return y => { throw new EntityAccessException(typeof(T),"Operation Denied on Entity " + typeof(T).FullName ,"Not Supported Rule"); };
            }
        }

        public EntityPropertyRulesCreator(EntityPropertyRules r)
        {
            this.Rules = r;

            if (r.ReadRule == null) {
                SetRead(this.NotSupportedRule);
            }
            if (r.WriteRule == null) {
                SetWrite(this.NotSupportedRule, false);
            }
            if (r.DeleteRule == null) {
                SetDelete(this.NotSupportedRule);
            }
        }

        public void SetFullControl(Func<object,Expression<Func<T, bool>>> rule)
        {
            Rules.ReadRule = rule;
            Rules.WriteRule = rule;
            Rules.DeleteRule = rule;
        }

        public void SetRead(Func<object, Expression<Func<T, bool>>> readRule) {
            Set(readRule, null, false, null);
        }

        public void SetWrite(Func<object, Expression<Func<T, bool>>> writeRule, bool alsoDelete = false )
        {
            Set(null, writeRule, alsoDelete, null);
        }

        public void SetDelete(Func<object, Expression<Func<T, bool>>> deleteRule)
        {
            Set(null, null, false, deleteRule);
        }

        public void Set(
            Func<object, Expression<Func<T, bool>>> readRule,
            Func<object, Expression<Func<T, bool>>> writeRule = null,
            bool useWriteForDelete = true, Func<object, Expression<Func<T, bool>>> deleteRule = null)
        {
            if (useWriteForDelete)
            {
                deleteRule = writeRule;
            }
            if (readRule != null)
            {
                Rules.ReadRule = readRule;
            }
            if (writeRule != null)
            {
                Rules.WriteRule = writeRule;
            }
            if (deleteRule != null)
            {
                Rules.DeleteRule = deleteRule;
            }
        }

        private void SetProperty(string property, SerializeMode mode)
        {
            Rules[property] = mode;
        }

        public void SetProperty(SerializeMode mode, params Expression<Func<T, object>>[] plist)
        {
            foreach (var exp in plist)
            {
                SetProperty(GetNameFrom(exp.Body), mode);
            }
        }

        private static string GetNameFrom(Expression exp)
        {


            UnaryExpression un = exp as UnaryExpression;
            if (un != null)
            {
                return GetNameFrom(un.Operand);
            }

            MemberExpression me = exp as MemberExpression;
            if (me != null)
            {
                string name = me.Member.Name;

                string parent = GetNameFrom(me.Expression);
                if (parent != null)
                {
                    name = parent + "." + name;
                }
                return name;
            }
            return null;
        }


    }

    //public class SecurityContextCreator<TSC>
    //    where TSC:BaseSecurityContext
    //{

        

    //    private Dictionary<Type, EntityPropertyRules> sc = null;
    //    private bool Admin = false;

    //    internal SecurityContextCreator(Dictionary<Type, EntityPropertyRules> sc, bool admin)
    //    {
    //        this.sc = sc;
    //        this.Admin = admin;
    //    }

    //    public EntityPropertyRulesCreator<TSC,T> CreateRule<T>()
    //    {

    //        EntityPropertyRules r = null;
    //        Type type = typeof(T);
    //        if (sc.TryGetValue(type, out r))
    //            return new EntityPropertyRulesCreator<TSC,T>(r);
    //        r = BaseSecurityContext.DefaultEntityPropertyRules(type, Admin);
    //        sc[type] = r;
    //        return new EntityPropertyRulesCreator<TSC,T>(r);
    //    }



    //    internal protected virtual void OnCreate()
    //    {

    //    }
    //}
}
