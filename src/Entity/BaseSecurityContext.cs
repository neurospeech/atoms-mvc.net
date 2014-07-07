using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
using NeuroSpeech.Atoms.Mvc.Entity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{

    public abstract class BaseSecurityContext<TC> : BaseSecurityContext
    {
        protected new EntityPropertyRulesCreator<T, TC> CreateRule<T>() 
            where T:class
        {
            return CreateRule<T, TC>();
        }
    }


    /// <summary>
    /// Stores generic security context and a default implementation
    /// </summary>
    public abstract class BaseSecurityContext
    {


        /// <summary>
        /// Initializes security context
        /// </summary>
        public BaseSecurityContext()
        {
        }

        /// <summary>
        /// Must be set only for admin operations, in which every entity property is accessible
        /// </summary>
        public bool IgnoreSecurity { get; protected set; }



        /// <summary>
        /// Security rules for accessing entity properties
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EntityPropertyRules this[Type type]
        {
            get
            {
                return Rules[type];
            }
        }

        protected EntityPropertyRulesCreator<T,object> CreateRule<T>()
            where T : class
        {
            return CreateRule<T, object>();
        }

        protected EntityPropertyRulesCreator<T, TC> CreateRule<T, TC>() 
            where T:class
        {
            EntityPropertyRules r = null;
            Type type = typeof(T);
            if (!Rules.TryGetValue(type, out r))
            {
                r = DefaultEntityPropertyRules(type, IgnoreSecurity);
                Rules[type] = r;
            }
            return new EntityPropertyRulesCreator<T, TC>(r);
        }



        public static EntityPropertyRules DefaultEntityPropertyRules(Type type, bool admin = false)
        {
            EntityPropertyRules list = new EntityPropertyRules(type);

            foreach (var item in type.GetEntityProperties(true))
            {
                list.SetMode(item.Name,SerializeMode.ReadWrite);
            }

            if (admin) {
                foreach (var item in type.GetEntityProperties(false))
                {
                    list.SetMode(item.Name,SerializeMode.ReadWrite);
                }
            }

            return list;
        }


        internal protected Dictionary<Type, EntityPropertyRules> Rules = new Dictionary<Type, EntityPropertyRules>();


        public Expression<Func<T, bool>> GetReadRule<T>(ISecureRepository db)
            where T : class
        {
            return GetRule<T>(r => r.ReadRule, db);
        }

        public Expression<Func<T, bool>> GetWriteRule<T>(ISecureRepository db)
            where T : class
        {
            return GetRule<T>(r => r.WriteRule, db);
        }
        public Expression<Func<T, bool>> GetDeleteRule<T>(ISecureRepository db)
            where T : class
        {
            return GetRule<T>(r => r.DeleteRule, db);
        }

        public Expression<Func<T, bool>> GetRule<T>(Func<EntityPropertyRules, object> r, ISecureRepository db)
        where T : class
        {
            Type type = typeof(T);
            EntityPropertyRules rule = null;
            Rules.TryGetValue(type, out rule);
            if (rule == null)
            {
                //if (type.BaseType != null && type.BaseType.IsClass)
                //{
                //    object retVal = GenericMethods.InvokeGeneric(this, "GetRule", new Type[] { type.BaseType }, r, db);
                //    if (retVal != null)
                //        return (Expression<Func<T, bool>>)retVal;
                //}

                if (IgnoreSecurity)
                    return x => true;

                throw new EntityAccessException(type, "No rule found for type " + type.FullName, "");
            }
            //Func<FilterContext, Expression<Func<T, bool>>> f = r(rule) as Func<FilterContext, Expression<Func<T, bool>>>;
            dynamic f = r(rule);
            if (f == null)
            {
                if (IgnoreSecurity)
                    return x => true;
                throw new EntityAccessException(type, "No rule found for type " + type.FullName, "");
            }
            //dynamic a = new FilterContext { Context = this, DB = db };
            dynamic ddb = db;
            return f(ddb);
        }

        public void VerifyEntityModify<T>(T item, IEnumerable<string> modifiedProperties = null)
            where T : class
        {

            Type entityType = typeof(T);

            // no need to verify modified properties (because we dont have it)
            if (modifiedProperties == null)
                return;

            EntityPropertyRules pr = this[entityType];
            if (pr == null)
                return;

            List<string> errors = new List<string>();

            foreach (var p in modifiedProperties)
            {
                if (pr[p] == SerializeMode.None)
                {
                    errors.Add(p);
                }
            }
            if (errors.Count > 0)
            {
                throw new EntityAccessException(typeof(T), string.Format("Can not modify " + string.Join(",", errors)) + " in " + entityType.FullName, entityType.FullName);
            }
        }

        internal void ValidateAfterSave(ISecureRepository db, ChangeSet cs)
        {
            if (IgnoreSecurity)
                return;
            foreach (var g in cs.UpdatedEntities)
            {
                object entity = g.Entity;
                Type type = entity.GetType();
                IRepositoryObject iro = entity as IRepositoryObject;
                if (iro != null) {
                    type = iro.ObjectType;
                }
                GenericMethods.InvokeGeneric(this, "VerifySourceEntity", type, db, entity, false);
            }
        }

        /// <summary>
        /// Verifies Entity along with Filter Expression, after filtering, Entity should result in the same entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="entity"></param>
        /// <param name="deleted"></param>
        public void VerifySourceEntity<T>(ISecureRepository db, T entity, bool deleted) where T : class
        {
            Type type = typeof(T);
            ParameterExpression pe = Expression.Parameter(type);
            Expression exp = null;
            foreach (var p in type.GetEntityProperties(true))
            {
                var e = Expression.Equal(Expression.Property(pe, p.Property), Expression.Constant(p.GetValue(entity)));
                exp = exp == null ? e : Expression.And(exp, e);
            }
            Expression<Func<T, bool>> key = Expression.Lambda<Func<T, bool>>(exp, pe);
            Expression<Func<T, bool>> f = null;
            f = deleted ? GetDeleteRule<T>(db) : GetWriteRule<T>(db);
            IQueryable<T> q = db.Query<T>();
            q = q.Where(f).Where(key);

            T item = q.FirstOrDefault();

            if (item != entity)
            {
                throw new EntityAccessException(type, "Can not modify Entity " + type.FullName, "");
            }
        }

        internal void ValidateBeforeSave(ISecureRepository db, ChangeSet cs)
        {
            if (IgnoreSecurity)
                return;
            foreach (ChangeSet.ChangeEntry item in cs.UpdatedEntities)
            {
                var entity = item.Entity;
                var type = item.Entity.GetType();
                var iro = entity as IRepositoryObject;
                if (iro != null) {
                    type = iro.ObjectType;
                }
                GenericMethods.InvokeGeneric(this, "VerifyEntityModify", type, entity, item.OriginalValues.Keys.ToList());
            }
            foreach (ChangeSet.ChangeEntry item in cs.Deleted)
            {
                var entity = item.Entity;
                var type = item.Entity.GetType();
                var iro = entity as IRepositoryObject;
                if (iro != null)
                {
                    type = iro.ObjectType;
                }
                GenericMethods.InvokeGeneric(this, "VerifySourceEntity", type, db, entity, true);
            }
        }
    }

}