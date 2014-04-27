using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Transactions;
using NeuroSpeech.Atoms.Entity;
using System.IO;

namespace NeuroSpeech.Atoms.Mvc
{
    public class CachedItemActionResult : ActionResult
    {
        public CacheItem CachedItem { get; set; }

        public CachedItemActionResult(CacheItem ci)
        {
            CachedItem = ci;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            byte[] data = CachedItem.Data;
            context.HttpContext.Response.ContentType = CachedItem.ContentType;
            context.HttpContext.Response.OutputStream.Write(data, 0, data.Length);
        }
    }
}
