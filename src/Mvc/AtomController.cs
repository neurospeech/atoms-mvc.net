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
using System.Web.WebPages;

namespace NeuroSpeech.Atoms.Mvc
{
    /// <summary>
    /// Controller that contains ISecureRepository and enables SecurityContext on it
    /// </summary>
    /// <typeparam name="TRepository">Secure Repository</typeparam>
	public abstract class AtomController<TRepository> : Controller
		where TRepository : ISecureRepository
	{

        /// <summary>
        /// SecureRepository associated with current controller
        /// </summary>
		public TRepository Repository { get; private set; }

        /// <summary>
        /// Deault Constructor
        /// </summary>
		public AtomController()
		{
			Repository = Activator.CreateInstance<TRepository>();


		}

        /// <summary>
        /// Global.asax must set SecurityContext after authentication, this method will set SecurityContext of current repository from HttpContext.Items["SecurityContext"]
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            Repository.SecurityContext = HttpContext.Items["SecurityContext"] as BaseSecurityContext;
            HttpContext.Items["Repository"] = Repository;
        }

        /// <summary>
        /// FormModel field passed by client
        /// </summary>
		protected Dictionary<string, object> _FormModel = null;

        /// <summary>
        /// Form model
        /// </summary>
		protected virtual Dictionary<string, object> FormModel {
			get {
				if (_FormModel == null) {
					string formModel = this.Request.Form["formModel"];
					if (string.IsNullOrWhiteSpace(formModel))
                    {
                        _FormModel = new Dictionary<string, object>();
                        return _FormModel;
                    }
                    JavaScriptSerializer jsr = new JavaScriptSerializer();
					_FormModel = (Dictionary<string, object>)jsr.Deserialize(formModel, typeof(object));
                    ParseDates(_FormModel);
				}
				return _FormModel;
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="optional"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
		protected T FormValue<T>(string name, bool optional = false, T defValue = default(T)) {
			object val = null;
			if (!FormModel.TryGetValue(name, out val)) { 
				if(!optional)
					throw new InvalidOperationException(name + " is required");
				return defValue;
			}
            if (val == null)
                return defValue;
			return (T)Convert.ChangeType(val, typeof(T));
		}




		private void ParseDates(Dictionary<string, object> data)
		{
			foreach (var item in data.ToList())
			{
				Dictionary<string, object> childObject = item.Value as Dictionary<string, object>;
				if (childObject != null) {
					ParseDates(childObject);
					continue;
				}

				string val = item.Value as string;
				if (val == null)
					continue;

				if (val.StartsWith("/Date(") && val.EndsWith(")/")) { 
					// change date..
					object date = AtomJavaScriptSerializer.ToDateTime(val);
					data[item.Key] = date;
				}
			}
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="values"></param>
        public virtual void LoadModel(object model, AtomDictionary values) {
            LoadModel(model, values.InternalDictionary);
        }

        /// <summary>
        /// Loads model ignoring ScriptIgnore and XmlIgnore Attributes...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        public virtual void LoadModel(object model, Dictionary<string, object> data = null)
        {
            if (data == null) {
                data = FormModel;
            }

			IJavaScriptDeserializable d = model as IJavaScriptDeserializable;
			if (d != null) {
				d.Deserialize(data);
				return;
			}

			Type type = model.GetType();
			foreach (var item in data)
			{
				PropertyInfo p = type.GetProperty(item.Key);
				if (p == null)
					continue;
                if (!p.CanWrite)
                    continue;
				object val = item.Value;
				if (val != null)
				{
					Type pt = p.PropertyType;

                    if (pt.IsGenericType)
                    {
                        //if (pt.GetGenericTypeDefinition() == typeof(List<>)) {

                        //    object dest = p.GetValue(model, null);
                        //    if (dest != null) { 

                        //    }
                        //    continue;
                        //}

                        if (pt.GetGenericTypeDefinition() == typeof(System.Nullable<>))
                        {
                            pt = pt.GetGenericArguments()[0];
                        }
                    }
					if (pt == typeof(DateTime))
					{
						string dt = val.ToString();
						if (dt.StartsWith("/Date"))
						{
							val = AtomJavaScriptSerializer.ToDateTime(dt);
						}
						else
						{
							// parse the UTC time 
							val = DateTime.Parse(dt);
						}
					}
					if (pt == typeof(Guid))
					{
						val = Guid.Parse((string)val);
					}
					if (val is IDictionary<string, object>)
					{
						var src = p.GetValue(model, null);
						if (src != null) {
							LoadModel(src, (Dictionary<string,object>)val);
						}
						continue;
					}
					else
					{
						val = Convert.ChangeType(val, pt);
					}
				}

				object oldValue = p.GetValue(model, null);

				if (oldValue == val)
					continue;
				if (oldValue != null && val != null && val.Equals(oldValue))
					continue;

				p.SetValue(model, val, null);
			}
		}

        /// <summary>
        /// Turning of Caching for POST by default
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);

            if (string.Equals(Request.HttpMethod,"POST", StringComparison.OrdinalIgnoreCase))
            {
                Response.Cache.SetNoServerCaching();
                Response.Cache.SetNoStore();
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

        }

        /// <summary>
        /// Enabling dynamic model before executing action
        /// </summary>
        /// <param name="filterContext"></param>
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{


			base.OnActionExecuting(filterContext);

			foreach (KeyValuePair<string,object> item in filterContext.ActionParameters.ToList())
			{
				if (item.Value == null)
					continue;
				Type itemType = item.Value.GetType();
				if (itemType == typeof(object))
				{
                    IJavaScriptDeserializable d = new AtomDictionary();
                    d.Deserialize(FormModel);
                    filterContext.ActionParameters[item.Key] = d;
					continue;
				}
				if (itemType.IsValueType || itemType == typeof(String))
					continue;
                LoadModel(item.Value);
			}

		}


		#region protected override void  Dispose(bool disposing)
        /// <summary>
        /// Dispose repository here
        /// </summary>
        /// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			Repository.Dispose();
		}
		#endregion

        /// <summary>
        /// Send client HTTP Error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
		protected ActionResult JsonError(string error)
		{
			return new JsonErrorResult(error);
		}

        /// <summary>
        /// Send client JSON as string response
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected ActionResult JsonString(string result = "null") {
            return Content(result, "application/json; charset=utf-8");
        }


        /// <summary>
        /// Send Json filtered with SecurityContext
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
		protected ActionResult JsonResult<T>(T model)
		{
            AtomJavaScriptSerializer ajs = new AtomJavaScriptSerializer(Repository.SecurityContext, true);
            string content = ajs.Serialize(model);
            return Content(content, "application/json");
		}

		#region protected override void  OnException(ExceptionContext filterContext)
        /// <summary>
        /// All exception except AtomValidationException must be logged to server
        /// </summary>
        /// <param name="filterContext"></param>
		protected override void OnException(ExceptionContext filterContext)
		{
			if (this.Request.IsAjaxRequest()) {
                AtomValidationException ave = filterContext.Exception as AtomValidationException;
                if (ave == null)
                {
                    Log(filterContext.Exception);
                }
				filterContext.Result = JsonError(filterContext.Exception.Message);
				filterContext.ExceptionHandled = true;
				return;
			}
			base.OnException(filterContext);
		}
		#endregion

        /// <summary>
        /// Overrride and implement your own exception logging method here
        /// </summary>
        /// <param name="ex"></param>
		protected virtual void Log(Exception ex)
		{
			
		}

    }

    public class AtomValidationException : Exception {
        public AtomValidationException()
        {

        }

        public AtomValidationException(string msg): base(msg)
        {

        }
    }
}