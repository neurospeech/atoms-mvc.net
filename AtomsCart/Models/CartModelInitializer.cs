using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AtomsCart.Models
{
    public class CartModelInitializer : DropCreateDatabaseAlways<CartModel>
    {

        protected override void Seed(CartModel context)
        {
            using (context.CreateSecurityScope(null))
            {
                context.Accounts.Add(new Account
                {
                    AccountName = "Administrator",
                    Username = "admin",
                    Password = "admin"
                });

                context.Products.Add(new Product
                {
                    ProductName = "Samsung S4"
                });

                context.Products.Add(new Product
                {
                    ProductName = "Apple iPhone 5S"
                });



                base.Seed(context);
            }
        }
    }
}