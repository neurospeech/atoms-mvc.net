using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppFramework.Areas.Admin.Controllers
{
    public class HomeController : AuthorizedController
    {
        // GET: Admin/Home
        public ActionResult Index()
        {
            return View();
        }
    }
}