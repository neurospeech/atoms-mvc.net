using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppFramework.Infrastructure
{
    public class Utils
    {
        public static string SHA1(string input) {
            using (var sha1 = System.Security.Cryptography.SHA1.Create()) {
                return Convert.ToBase64String(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
            }
        }
    }
}