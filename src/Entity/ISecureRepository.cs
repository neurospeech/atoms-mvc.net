using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Entity
{
    public interface ISecureRepository : IDisposable
    {
        ///// <summary>
        ///// Retrieves ObjectStateEntry from underlying context
        ///// </summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //ObjectStateEntry GetEntry(object entity);



        BaseSecurityContext SecurityContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q"></param>
        /// <returns></returns>
        IQueryable<T> ApplyFilter<T>(IQueryable<T> q) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> Query<T>() where T:class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        IQueryable<T> Where<T>(Expression<Func<T, bool>> filter) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        IQueryable<T> NavigationQuery<T>(object entity, string property, bool collection) where T:class;

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="t"></param>
        ///// <returns></returns>
        //IQueryable Query(Type t);


        /// <summary>
        /// Adds entity to repository
        /// </summary>
        /// <param name="entity"></param>
        void AddEntity(object entity);

        ///// <summary>
        ///// Adds or Updates given Entity to Entity Set, based on type, EntitySet or DbSet will be
        ///// determined at runtime
        ///// </summary>
        ///// <param name="entity"></param>
        //object ModifyEntity(object entity);

        /// <summary>
        /// Deletes object from database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        object DeleteEntity(object entity);

        /// <summary>
        /// Final call to SaveChanges wrapped up in Transaction to validate after changes LINQ Rules
        /// </summary>
        /// <returns></returns>
        IEnumerable<object> Save();


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<object>> SaveAsync();

    }
}
