using NeuroSpeech.Atoms.Entity;
using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc.Entity
{
    public class AtomDbContext : DbContext, ISecureRepository
    {

        public AtomDbContext()
        {
        }


        public AtomDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        


        public IAuditContext AuditContext { get; set; }

        public BaseSecurityContext SecurityContext
        {
            get;
            set;
        }

        public IQueryable<T> ApplyFilter<T>(IQueryable<T> q) where T : class
        {
            if (SecurityContext != null && !SecurityContext.IgnoreSecurity)
            {
                var rule = SecurityContext.GetReadRule<T>(this);
                if (rule != null)
                {
                    q = q.Where(rule);
                }
            }
            return q;
        }

        public IQueryable<T> Query<T>() where T : class
        {
            return this.ApplyFilter(this.Set<T>());
        }

        public IQueryable<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> filter) where T : class
        {
            return this.Query<T>().Where(filter);
        }

        public IQueryable<T> NavigationQuery<T>(object entity, string property, bool collection) where T : class
        {
            if (collection)
                return (IQueryable<T>)this.Entry(entity).Collection(property).Query();
            return (IQueryable<T>)this.Entry(entity).Reference(property).Query();
        }

        public void AddEntity(object entity)
        {
            Type type = entity.GetType();
            this.Set(type).Add(entity);
        }


        public object DeleteEntity(object entity)
        {
            Type type = entity.GetType();
            this.Set(type).Remove(entity);
            return entity;
        }

        public int Save()
        {
            using (var tx = this.Database.BeginTransaction())
            {
                ChangeSet cs = new ChangeSet(this);

                if (SecurityContext != null)
                {
                    SecurityContext.ValidateBeforeSave(this, cs);
                }

                if (AuditContext != null)
                {
                    cs.BeginAudit();
                }

                int result = this.SaveChanges();

                if (SecurityContext != null)
                {
                    SecurityContext.ValidateAfterSave(this, cs);
                }


                if (AuditContext != null) {
                    cs.EndAudit(AuditContext);
                }
                tx.Commit();

                return result;
            }
        }

        private static GenericMethods GenericMethods = new GenericMethods();

        public async Task VerifySourceEntity<T>(T entity, bool deleted = false)
            where T:class
        {
            var rule = deleted ? SecurityContext.GetDeleteRule<T>(this) : SecurityContext.GetWriteRule<T>(this);
            IQueryable<T> q = this.Set<T>();
            var e = await q.Where(rule).WhereCopy(entity).FirstOrDefaultAsync();
            if (e != entity) {
                Type type = typeof(T);
                throw new EntityAccessException(type, "Can not " + (deleted ? "Delete" : "Modify" ) +  " Entity " + type.FullName, "");
            }
        }

        public async Task<int> SaveAsync()
        {

            using (var tx = this.Database.BeginTransaction())
            {
                ChangeSet cs = new ChangeSet(this);

                if (!(SecurityContext == null || SecurityContext.IgnoreSecurity))
                {
                    foreach (var item in cs.UpdatedEntities)
                    {
                        BaseSecurityContext.GenericMethods.InvokeGeneric(
                            SecurityContext, 
                            "VerifyEntityModify", 
                            item.EntityType, 
                            item.Entity, 
                            item.OriginalValues.Keys.ToList());
                    }

                    foreach (var item in cs.Deleted)
                    {
                        await (Task)GenericMethods.InvokeGeneric(this,
                            "VerifySourceEntity", item.EntityType, item.Entity, true);
                    }
                }

                if (AuditContext != null)
                {
                    cs.BeginAudit();
                }

                try
                {
                    int result = await this.SaveChangesAsync();

                    if (!(SecurityContext == null || SecurityContext.IgnoreSecurity))
                    {
                        foreach (var item in cs.UpdatedEntities)
                        {
                            await (Task)GenericMethods.InvokeGeneric(this,
                                "VerifySourceEntity", item.EntityType, item.Entity, false);
                        }
                    }


                    if (AuditContext != null) {
                        await cs.EndAuditAsync(AuditContext);
                    }

                    tx.Commit();

                    return result;
                }
                catch (DbEntityValidationException ve)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in ve.EntityValidationErrors.Where(x=>!x.IsValid))
                    {
                        foreach (var error in item.ValidationErrors)
                        {
                            sb.AppendLine( item.Entry.Entity.GetType() + "." + error.PropertyName + ": " + error.ErrorMessage);
                        }
                    }
                    throw new AtomValidationException(sb.ToString());
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) {
                var ac = this.AuditContext;
                if (ac != null) {
                    ac.Dispose();
                    this.AuditContext = null;
                }
            }
        }


        /// <summary>
        /// Creates new Security Scope
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        public IDisposable CreateSecurityScope(BaseSecurityContext sc = null) 
        {
            var current = this.SecurityContext;
            this.SecurityContext = sc;
            return new DisposableAction(() => {
                this.SecurityContext = current;
            });
        }


        


    }

    public class DisposableAction : IDisposable
    {

        private Action action;

        public DisposableAction(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action();
        }
    }

}
