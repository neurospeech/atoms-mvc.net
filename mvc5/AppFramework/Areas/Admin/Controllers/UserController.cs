using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AppFramework.Infrastructure;
using System.Web.Security;

namespace AppFramework.Areas.Admin.Controllers
{
    public class UserController : AdminBaseController
    {
        // GET: Admin/User
        public ActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Login(LoginModel model)
        {
            var user = await Repository.ApiUsers.FirstOrDefaultAsync(x => x.EmailAddress == model.EmailAddress);
            if (user == null)
                return new HttpStatusCodeResult(500, "User not found");
            if (user.PasswordSHA1 != Utils.SHA1(model.Password))
                return new HttpStatusCodeResult(401, "Invalid password");

            var cookie = FormsAuthentication.GetAuthCookie(user.UserID.ToString(), model.RememberMe);
            cookie.Name = ".Api-Admin";
            Response.SetCookie(cookie);

            return Json(new { 
                ReturnUrl = model.ReturnUrl
            });
        }

    }

    public class LoginModel
    {
        public string EmailAddress { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }
}