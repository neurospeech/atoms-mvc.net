using NeuroSpeech.Atoms.Mvc.Entity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AppFramework.Models
{
    public class AppFrameworkContext : DbContext
    {



    }

    public abstract class BaseFrameworkContext : AtomDbContext {

        public long UserID { get; set; }

    }
}