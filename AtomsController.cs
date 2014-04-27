using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Filters;

namespace NeuroSpeech.Atoms.Mvc
{

    /// <summary>
    /// Encapsulates Secure Repository
    /// </summary>
    /// <typeparam name="TRepository"></typeparam>
    public abstract class AtomsController<TRepository> : Controller
        where TRepository:class,ISecureRepository
    {

        private TRepository _Repository = null;

        /// <summary>
        /// 
        /// </summary>
        public TRepository Repository {
            get {
                return _Repository;
            }
        }

        /// <summary>
        /// Creates associated repository for this controller
        /// </summary>
        /// <returns></returns>
        protected virtual TRepository CreateRepository()
        {
            return Activator.CreateInstance<TRepository>();
        }

        protected abstract BaseSecurityContext AttachSecurityContext(AuthenticationContext context);

        /// <summary>
        /// Creates Secure Repository
        /// </summary>
        /// <param name="requestContext"></param>
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);

            _Repository = CreateRepository();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnAuthentication(System.Web.Mvc.Filters.AuthenticationContext filterContext)
        {
            base.OnAuthentication(filterContext);
            BaseSecurityContext sc = AttachSecurityContext(filterContext);
            _Repository.Initialize(User.Identity.Name, sc);
        }

        /// <summary>
        /// Disposes the repository
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (_Repository != null) {
                    _Repository.Dispose();
                    _Repository = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
