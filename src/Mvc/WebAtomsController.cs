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

    public interface ISecureRepositoryController {
        ISecureRepository Repository { get; }
    }

	public abstract class WebAtomsController<TOC> : Controller, ISecureRepositoryController
		where TOC : ISecureRepository
	{
		public TOC ObjectContext { get; private set; }

		public WebAtomsController()
		{
			ObjectContext = Activator.CreateInstance<TOC>();


		}

		protected Dictionary<string, object> _FormModel = null;

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

		protected T FormValue<T>(string name, bool optional = false, T defValue = default(T)) {
			object val = null;
			if (!FormModel.TryGetValue(name, out val)) { 
				if(!optional)
					throw new InvalidOperationException(name + " is required");
				return defValue;
			}
            if (val == null)
                return defValue;
            //string v = (val as string) ?? val.ToString();
            //if (optional && string.IsNullOrWhiteSpace(v))
            //    return defValue;
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
					object date = WebAtomsMvcHelper.ToDateTime(val);
					data[item.Key] = date;
				}
			}
		}

        //[Obsolete("",true)]
        //public void LoadModel<T>(T model) 
        //    where T:class 
        //{
        //        LoadModel(model, null);
        //}


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
							val = WebAtomsMvcHelper.ToDateTime(dt);
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

		protected AtomQueryableResult<T> Where<T>(Expression<Func<T, bool>> filter = null)
			where T : class
		{
			Response.Cache.SetCacheability(HttpCacheability.NoCache);
			return new AtomQueryableResult<T>(ObjectContext.Where(filter), ObjectContext.SecurityContext);
		}

        //protected virtual ObjectQuery<T> ApplyFilter<T>(ObjectQuery<T> oset) where T : class {
        //    ISecureRepository ios = ObjectContext as ISecureRepository;
        //    if (ios != null)
        //        return ios.ApplyFilter(oset);
        //    return oset;
        //}


        protected virtual void LoadUserInformation(ActionExecutingContext filterContext)
        {

        }

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


		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{

            var sc = CreateSecurityContext();
            if (sc != null) {
                ObjectContext.SecurityContext = sc;
            }

            if (User.Identity.IsAuthenticated) {
                LoadUserInformation(filterContext);
            }

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
				//MethodInfo m = GetLoadMethod.MakeGenericMethod(itemType);
				//m.Invoke(this, new object[] { item.Value });
                LoadModel(item.Value);
			}

		}

        protected virtual BaseSecurityContext CreateSecurityContext()
        {
            return null;
        }


		#region protected override void  Dispose(bool disposing)
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			ObjectContext.Dispose();
		}
		#endregion

		protected ActionResult JsonError(string error)
		{
			return new JsonErrorResult(error);
		}

        protected ActionResult JsonString(string result = "null") {
            return Content(result, "application/json; charset=utf-8");
        }

        protected ActionResult JsonEmpty() {
            return JsonString("{}");
        }

        protected ActionResult JsonEmptyArray() {
            return JsonString("[]");
        }


		protected ActionResult JsonResult<T>(T model)
		{
			return new AtomResult<T>(model);
		}

        protected ActionResult InvokeTransaction(Func<TOC, ActionResult> f) {
            ActionResult result = null;
            using (TransactionScope scope = new TransactionScope()) {
                try {
                    result = f(ObjectContext);
                }
                catch (Exception ex) {
                    Log(ex);
                    if (this.Request.IsAjaxRequest()) {
                        return JsonError(ex.Message);
                    }
                    throw;
                }
                scope.Complete();
            }

            return result;
        }


		protected ActionResult Invoke(Func<TOC, ActionResult> f)
		{
            try
            {
                return f(ObjectContext);
            }
            catch (TargetInvocationException tex) {
                if (tex.InnerException != null && tex.InnerException is AtomValidationException) {
                    return JsonError(tex.InnerException.Message);
                }
                Log(tex);
                if (this.Request.IsAjaxRequest() || string.Compare(this.Request.HttpMethod, "post", true) == 0) {
                    return JsonError(tex.Message);
                }
                throw;
            }
            catch (AtomValidationException ex)
            {
                return JsonError(ex.Message);
            }
            catch (Exception ex)
            {
                Log(ex);
                if (this.Request.IsAjaxRequest() || string.Compare(this.Request.HttpMethod, "post", true) == 0)
                {
                    return JsonError(ex.Message);
                }
                throw;
            }
		}

        //protected ActionResult Invoke(Func<ActionResult> f)
        //{
        //    try
        //    {
        //        return f();
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(ex);
        //        if (this.Request.IsAjaxRequest())
        //        {
        //            return JsonError(ex.Message);
        //        }
        //        throw;
        //    }
        //}

        protected virtual ActionResult InvokeCached(Func<TOC, DateTime> expiryGenerator,
                Func<TOC, ActionResult> invoker) 
        {
            return InvokeCached(db => Request.Url.ToString(), expiryGenerator, invoker);
        }

        protected virtual ActionResult InvokeCached(
                Func<TOC, string> cacheKeyGenerator, 
                Func<TOC,DateTime> expiryGenerator,
                Func<TOC, ActionResult> invoker) 
        {

            string userType = HttpContext.Items["UserType"] as string;

            var dm = DisplayModeProvider.Instance.Modes.FirstOrDefault(x => x.CanHandleContext(this.HttpContext));
            string view = null;
            if (dm != null && !string.IsNullOrWhiteSpace(dm.DisplayModeId)) {
                view = dm.DisplayModeId;
            }

            string cacheKeyFolder = string.Format("{0}\\{1}\\{2}", Request.Url.Host, userType ?? "anonymous", view ?? "desktop");
            string key = cacheKeyGenerator(ObjectContext);
            DateTime lastModified = expiryGenerator(ObjectContext);

            string cacheKey = cacheKeyFolder + "\\" + NameHashProvider.Instance.GetHashCode(key) + "\\" + key;


            var tf = TempFolderCache.Instance;
            if (tf == null) {
                tf = new TempFolderCache();
            }

            CacheItem cachedOutput = tf.Get(cacheKey) as CacheItem;
            if (cachedOutput != null && cachedOutput.LastModified != lastModified) {
                tf.Remove(cacheKey);
                cachedOutput = null;
            }
            if (cachedOutput != null)
            {
                Response.AddHeader("x-app-cache-hit", cacheKey);
                return new CachedItemActionResult(cachedOutput);
            }
            else {
                Response.AddHeader("x-app-cache-miss", cacheKey);
                HttpContext.Items["Cache-Output-Key"] = cacheKey;
                ResponseFilterStream ms = new ResponseFilterStream(Response.Filter);
                HttpContext.Items["Cache-Output-Stream"] = ms;
                Response.Filter = ms;
                ms.Closed = () =>
                {
                    string ck = ControllerContext.HttpContext.Items["Cache-Output-Key"] as string;
                    if (ck == null)
                        return;
                    
                    CacheItem ci = new CacheItem
                    {
                        LastModified = lastModified,
                        ContentType = Response.ContentType,
                        Data = ms.Buffer
                    };

                    tf.Add(ck, ci, DateTime.UtcNow.AddDays(30));

                    /*HttpContext.Cache.Add(
                        cacheKey,
                        ci,
                        null,
                        DateTime.UtcNow.AddYears(1),
                        TimeSpan.Zero,
                        System.Web.Caching.CacheItemPriority.NotRemovable, null);*/
                };

            }
            return Invoke(invoker);
        }



		#region protected override void  OnException(ExceptionContext filterContext)
		protected override void OnException(ExceptionContext filterContext)
		{

            // dont cache anything...
            filterContext.HttpContext.Items.Remove("Cache-Output-Key");

			if (this.Request.IsAjaxRequest()) {
				Log(filterContext.Exception);
				filterContext.Result = JsonError(filterContext.Exception.Message);
				filterContext.ExceptionHandled = true;
				return;
			}
			base.OnException(filterContext);
		}
		#endregion


		protected virtual void Log(Exception ex)
		{
			
		}

        protected ActionResult OnBulkCommand<T>(string ids, Action<T> action)
            where T : class
        {
            foreach (object pk in ids.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var item = ObjectContext.QueryByKey<T>(pk).FirstOrDefault();
                if (item == null)
                    continue;
                action(item);
            }
            ObjectContext.Save();
            return JsonResult("");
        }

        ISecureRepository ISecureRepositoryController.Repository
        {
            get {
                return this.ObjectContext;
            }
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