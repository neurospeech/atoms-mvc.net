using AtomsCart.Models;
using NeuroSpeech.Atoms.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AtomsCart.Roles
{
    public class UserRoleSecurityContext: BaseSecurityContext<CartModel>
    {

        public static UserRoleSecurityContext Instance = new UserRoleSecurityContext();

        private UserRoleSecurityContext():base(false)
        {

            var account = CreateRule<Account>();

            account.SetDelete(account.NotSupportedRule);

            account.SetWrite(y => 
                x => x.AccountID == y.UserID);

            account.SetRead(y => 
                x => x.AccountID == y.UserID);

            account.SetProperty(SerializeMode.Read, 
                x => x.AccountID, 
                x => x.AccountName);

            account.SetProperty(SerializeMode.ReadWrite, x => x.AccountName);

            var product = CreateRule<Product>();
            product.SetFullControl(product.NotSupportedRule);
            product.SetRead(y => x => true);
            product.SetProperty(SerializeMode.Read, x => x.ProductName);


            var order = CreateRule<Order>();

            order.SetFullControl(y => 
                x => x.CustomerID == y.UserID);

            order.SetDelete(order.NotSupportedRule);

            order.SetProperty(SerializeMode.ReadWrite, 
                x => x.CustomerID, 
                x => x.Description, 
                x => x.Total, 
                x => x.DateCreated,
                x => x.DateUpdated);


            var orderItem = CreateRule<OrderItem>();

            orderItem.SetFullControl(y => 
                x => x.Order.CustomerID == y.UserID);

            orderItem.SetDelete(orderItem.NotSupportedRule);

            orderItem.SetProperty(SerializeMode.ReadWrite, 
                x => x.OrderID, 
                x => x.ProductID, 
                x => x.Amount, 
                x=> x.Quantity, 
                x=> x.Total);
        }
    }
}