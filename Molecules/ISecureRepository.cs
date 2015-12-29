using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Molecules
{
    public interface ISecureRepository
    {
        void Add<T>(T entity);
        void Update<T>(T entity);
        void Delete<T>(T entity);

        IQueryable<T> Query<T>();
        Task<int> SaveChanges();
        ISecureTransaction CreateTransaction();

        ISecurityContext SecurityContext { get; }

        IDisposable CreateSecurityContext(ISecurityContext context);
    }

    public interface ISecureTransaction
    {
        void Complete();
    }

    public interface ISecurityContext {

        bool CanModify(object entity);
        bool CanDelete(object entity);

        IQueryable Filter(IQueryable query);

    }
}
