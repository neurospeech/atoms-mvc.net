using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace AppFramework.Areas.Admin.Controllers
{
    public class AuthorizedController : AdminBaseController
    {

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (!IsAuthenticated(filterContext))
            {
                filterContext.Result = new RedirectResult("/admin/user/login");
            }
        }

        private bool IsAuthenticated(AuthorizationContext filterContext)
        {
            try
            {
                HttpCookie cookie = filterContext.HttpContext.Request.Cookies[".Api-Admin"];
                if (cookie != null)
                {
                    var ticket = FormsAuthentication.Decrypt(cookie.Value);
                    if (ticket.Expired)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch 
            {
                return false;
            }
            return false;

        }

    }
}