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
            if (SecurityContext != null)
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


        public async Task<int> SaveAsync()
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

                try
                {
                    int result = await this.SaveChangesAsync();

                    if (SecurityContext != null)
                    {
                        SecurityContext.ValidateAfterSave(this, cs);
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



    }

}
