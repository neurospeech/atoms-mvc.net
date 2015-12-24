using AtomsCart.Models;
using AtomsCart.Roles;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AtomsCart.Controllers
{

    
    public class APIController : AtomEntityController<CartModel>
    {

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

            Repository.SecurityContext = UserRoleSecurityContext.Instance;
        }

    }
}