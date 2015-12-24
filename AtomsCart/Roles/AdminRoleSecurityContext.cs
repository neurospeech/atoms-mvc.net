using AtomsCart.Models;
using NeuroSpeech.Atoms.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AtomsCart.Roles
{
    public class AdminRoleSecurityContext : BaseSecurityContext<CartModel>
    {

        public static AdminRoleSecurityContext Instance = new AdminRoleSecurityContext();

        private AdminRoleSecurityContext():base(true)
        {
        }
    }
}