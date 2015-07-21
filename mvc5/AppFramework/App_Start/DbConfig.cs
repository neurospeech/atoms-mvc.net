using AppFramework.Areas.Admin.Models;
using AppFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppFramework
{
    public class DbConfig
    {
        internal static void Setup()
        {
            using (AdminDbContext db = new AdminDbContext()) {
                db.Database.CreateIfNotExists();
                if (!db.ApiUsers.Any()) {


                    db.ApiUsers.Add(new ApiUser { 
                        EmailAddress = "admin@admin.com",
                        PasswordSHA1 = Utils.SHA1("$safe1234")
                    });
                    db.SaveChanges();
                }
            }
        }
    }
}