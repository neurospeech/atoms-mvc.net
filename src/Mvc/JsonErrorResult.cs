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


namespace NeuroSpeech.Atoms.Mvc
{
    public class JsonErrorResult : ActionResult
    {
        public string Error { get; set; }
        public JsonErrorResult(string error)
        {
            this.Error = string.IsNullOrWhiteSpace(error) ? "Unknown Error" : error;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            string errorCode = Error.Split('\n', '\r').Where(x => !string.IsNullOrWhiteSpace(x)).FirstOrDefault();



            var Response = context.HttpContext.Response;
            Response.TrySkipIisCustomErrors = true;
            Response.StatusCode = 500;
            Response.StatusDescription = errorCode;
            Response.Status = "500 " + errorCode;
            Response.TrySkipIisCustomErrors = true;
            Response.Write(Error);

        }
    }
}
