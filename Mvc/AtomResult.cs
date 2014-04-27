using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using NeuroSpeech.Atoms.Linq;
using NeuroSpeech.Atoms.Entity;

namespace NeuroSpeech.Atoms.Mvc
{
    public class AtomResult<T> : ActionResult
    {
        public AtomResult(T result)
        {
            Result = result;
        }

        //public AtomResult(Exception ex)
        //{
        //	Error = ex.Message;
        //	ErrorDetails = ex.ToString();
        //}

        public T Result { get; set; }


        #region public override void  ExecuteResult(ControllerContext context)
        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(null, true);
            context.HttpContext.Response.Write( js.Serialize(Result) );



        }
        #endregion


    }
}