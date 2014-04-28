using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
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
                EntityPropertyRules list = null;
                if (!Rules.TryGetValue(type, out list))
                {
                    list = CreateEntityPropertyRules(type);
                    Rules[type] = list;
                }
                return list;
            }
        }

        protected EntityPropertyRulesCreator<TContext,T> CreateRule<TContext,T>()
            where TContext:ISecureRepository
            where T : class
        {
            EntityPropertyRules r = null;
            Type type = typeof(T);
            if (!Rules.TryGetValue(type, out r))
            {
                r = BaseSecurityContext.DefaultEntityPropertyRules(type, IgnoreSecurity);
                Rules[type] = r;
            }
            return new EntityPropertyRulesCreator<TContext,T>(r);
        }


        protected virtual EntityPropertyRules CreateEntityPropertyRules(Type type)
        {
            EntityPropertyRules rules = DefaultEntityPropertyRules(type, IgnoreSecurity);
            return rules;
        }

        public static EntityPropertyRules DefaultEntityPropertyRules(Type type, bool admin = false)
        {
            EntityPropertyRules list = new EntityPropertyRules(type.Name, type.FullName);

            list["EntityKey"] = SerializeMode.None;
            list["EntityState"] = SerializeMode.None;

            // lock down.. dont allow anything...
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(type))
            {
                list[item.Name] = SerializeMode.None;
            }


            Type entityType = typeof(EntityObject);

            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(type))
            {
                if (!admin)
                {
                    if (item.Attributes.OfType<XmlIgnoreAttribute>().Any())
                        continue;
                    if (item.Attributes.OfType<ScriptIgnoreAttribute>().Any())
                        continue;
                }
                else
                {

                    if (!item.Attributes.OfType<EdmScalarPropertyAttribute>().Any())
                    {
                        if (item.Attributes.OfType<XmlIgnoreAttribute>().Any())
                            continue;
                        if (item.Attributes.OfType<ScriptIgnoreAttribute>().Any())
                            continue;
                    }

                }
                if (item.IsReadOnly)
                {
                    list[item.Name] = SerializeMode.Read;
                    continue;
                }
                Type propertyType = item.PropertyType;
                if (propertyType.IsEnum)
                    continue;
                if (propertyType.Name.StartsWith("EntityReference"))
                    continue;
                if (propertyType.Assembly == entityType.Assembly)
                    continue;

                Type ownerType = type;

                PropertyInfo info = ownerType.GetProperty(item.Name);
                if (info == null || info.DeclaringType == entityType)
                    continue;

                if (entityType.IsAssignableFrom(propertyType))
                {
                    if (ownerType.GetProperty(item.Name + "Reference") != null)
                    {
                        continue;
                    }
                }

                list[item.Name] = SerializeMode.ReadWrite;
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
                if (IgnoreSecurity)
                    return x => true;

                throw new EntityAccessException(type, "No rule found for type " + type.FullName, "");
            }

            dynamic f = r(rule);
            if (f == null)
            {
                if (IgnoreSecurity)
                    return x => true;
                throw new EntityAccessException(type, "No rule found for type " + type.FullName, "");
            }
            dynamic a = db;
            return f(a);
        }

        public void VerifyEntityModify<T>(T item, IEnumerable<string> modifiedProperties = null)
            where T : class
        {

            Type entityType = typeof(T);

            // no need to verify modified properties (because we dont have it)
            if (modifiedProperties == null)
                return;

            AtomEntity ae = item as AtomEntity;
            EntityPropertyRules pr = ae.SecurityRules ?? this[entityType];
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
            foreach (ObjectStateEntry item in cs.UpdatedEntities)
            {
                var entity = item.Entity;
                var type = item.Entity.GetType();
                GenericMethods.InvokeGeneric(this, "VerifyEntityModify", type, entity, item.GetModifiedProperties());
            }
            foreach (ObjectStateEntry item in cs.Deleted)
            {
                var entity = item.Entity;
                var type = item.Entity.GetType();
                GenericMethods.InvokeGeneric(this, "VerifySourceEntity", type, db, entity, true);
            }
        }
    }

    public abstract class BaseSecurityContext<TContext> : BaseSecurityContext 
        where TContext:ISecureRepository
    {

        protected EntityPropertyRulesCreator<TContext, T> CreateRule<T>()
            where T:class
        {
            return base.CreateRule<TContext, T>();
        }
    
    }

}