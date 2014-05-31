using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace NeuroSpeech.Atoms.Mvc
{
	public class AtomDataResult : ActionResult
	{
		public byte[] Data { get; set; }

		public string FileName { get; set; }

		public string ContentType { get; set; }

        public bool Inline { get; set; }

		public override void ExecuteResult(ControllerContext context)
		{
			var Response = context.HttpContext.Response;

			if (!string.IsNullOrWhiteSpace(ContentType)) {
				Response.ContentType = ContentType;
			}

			if (!string.IsNullOrWhiteSpace(FileName)) {
				Response.AddHeader("Content-Disposition", (Inline ? "inline" : "attachment") + ";filename=\"" + FileName + "\"");
			}

			Response.OutputStream.Write(Data, 0, Data.Length);
		}
	}
}
