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
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Threading.Tasks;
using NeuroSpeech.Atoms.Mvc.Entity;

namespace NeuroSpeech.Atoms.Mvc
{



    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOC"></typeparam>
    [ValidateInput(false)]
    public abstract class AtomEntityController<TOC> : AtomController<TOC>
        where TOC : ISecureRepository
    {

        

        private TOC db {
            get {
                return this.Repository;
            }
        }

        static GenericMethods GenericMethods = new GenericMethods();

        public virtual async Task<ActionResult> Parent(string table, string id, string property) {
            Type entityType = GetType(Repository, table);
            PropertyInfo p = entityType.GetProperty(property);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "ParentEntity", new Type[] { entityType,p.PropertyType }, p, id);
        }

        public virtual async Task<ActionResult> ParentEntity<T, TC>(PropertyInfo p, string id) 
            where T:class
            where TC:class
        {
            var aq = db.Query<T>().WhereKey(id);
            var tx = await aq.FirstOrDefaultAsync();
            var eq = db.ApplyFilter(db.NavigationQuery<TC>(tx,p.Name, false));
            return JsonResult(await eq.FirstOrDefaultAsync());
        }

        public virtual async Task<ActionResult> Children(string table, string id, string property, string query, string orderBy, string fields, int start = 0, int size = -1) {
            Type entityType = GetType(Repository, table);
            PropertyInfo p = entityType.GetProperty(property);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "ChildEntities", new Type[] { entityType, p.PropertyType.GetGenericArguments()[0]},
                p,
                id,
                query,
                orderBy,
                fields,
                start,
                size);

        }

        public virtual async Task<ActionResult> ChildEntities<T,CT>(PropertyInfo p, string id, string query, string orderBy, string fields, int start, int size) 
            where T:class 
            where CT:class
        {
            var aq = db.Query<T>().WhereKey(id);
            var tx = await aq.FirstOrDefaultAsync();
            var r = db.NavigationQuery<CT>(tx,p.Name, true);
            var q = Repository.ApplyFilter<CT>(r);

            q = q.WhereJsonQuery(query,db.SecurityContext);
            long total = 0;
            if (size != -1)
            {
                if (string.IsNullOrWhiteSpace(orderBy))
                    return JsonError("orderBy missing");
                q = q.OrderBy(orderBy);
                total = await q.CountAsync();
                q = q.Skip(start).Take(size);
            }
            else {
                if (!string.IsNullOrWhiteSpace(orderBy))
                    q = q.OrderBy(orderBy);
            }

            IQueryable rq = q;

            if (!string.IsNullOrWhiteSpace(fields)) {
                var d = PrepareFields<T>(fields);
                rq = q.SelectDynamic(d);
            }
            object result = await rq.ToListAsync();
            if (size != -1) {
                result = new { 
                    items = result,
                    total = total,
                    merge = true
                };
            }
            return JsonResult(result);
        }

        public virtual async Task<ActionResult> QueryEntity<T>(string query, string fields, string orderBy, int start, int size)
            where T : class
        {
            string includeList = Request.QueryString["include"] ?? "";
            if (!string.IsNullOrWhiteSpace(includeList))
            {
                IQueryable<T> q = db.Query<T>().WhereJsonQuery(query,db.SecurityContext);
                Type type = typeof(T);

                var propList = includeList.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                foreach (string inc in propList)
                {
                    q = q.Include(inc);
                }

                List<T> entites = await q.ToListAsync();

                Associations relEntities = new Associations();

                var f = relatedExpressions.GetOrAdd(type.FullName + ":" + includeList, key => CreateRelatedLoadExpression(type, propList));

                int i = 0;
                foreach (T entity in entites)
                {
                    f(relEntities, i++, entity);
                }

                var model = new
                {
                    items = entites,
                    total = entites.Count,
                    associations = relEntities
                };

                return JsonResult(model);
            }

            long total = 0;

            var aq = db.Query<T>();
            if (!string.IsNullOrWhiteSpace(query))
            {
                aq = aq.WhereJsonQuery(query, db.SecurityContext);
            }
            if (size != -1)
            {
                if (string.IsNullOrWhiteSpace(orderBy))
                    return JsonError("orderBy missing");
                aq = aq.OrderBy(orderBy);
                total = await aq.CountAsync();
                aq = aq.Skip(start).Take(size);
            }
            else {
                if (!string.IsNullOrWhiteSpace(orderBy)) {
                    aq = aq.OrderBy(orderBy);
                }
            }

            IQueryable rq = aq;

            if (!string.IsNullOrWhiteSpace(fields))
            {
                var d = PrepareFields<T>(fields);
                rq = aq.SelectDynamic(d);
            }

            object result = await rq.ToListAsync();
            if (size != -1) {
                result = new { 
                    items = result,
                    total = total,
                    merge = true
                };
            }
            return JsonResult(result);
        }

        private Dictionary<string, string> PrepareFields<T>(string fields) where T : class
        {
            var entityType = typeof(T);
            JavaScriptSerializer js = new JavaScriptSerializer();
            var d = (Dictionary<string, string>)js.Deserialize(fields, typeof(Dictionary<string, string>));

            List<string> errors = new List<string>();

            var sec = this.Repository.SecurityContext;
            if (sec != null && sec.IgnoreSecurity == false)
            {
                EntityPropertyRules pl = sec[entityType];
                foreach (var item in d)
                {
                    string field = string.IsNullOrWhiteSpace(item.Value) ? item.Key : item.Value;
                    var m = pl[field];
                    if (m == SerializeMode.None)
                        errors.Add(field);
                }
            }
            if (errors.Count > 0) {
                throw new EntityAccessException(entityType,"Access Denied for " + string.Join(", ", errors) + " in " + entityType.FullName, "");
            }
            return d;
        }

        public virtual async Task<ActionResult> GetEntity<T>(string query, string fields, string orderBy)
            where T : class
        {
            var aq = db.Query<T>().WhereJsonQuery(query,db.SecurityContext);
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                aq = aq.OrderBy(orderBy);
            }
            if (!string.IsNullOrWhiteSpace(fields))
            {
                var p = PrepareFields<T>(fields);
                return JsonResult((await aq.SelectDynamic(p).ToListAsync()).FirstOrDefault());
            }
            return JsonResult(await aq.FirstOrDefaultAsync());
        }

        public virtual async Task<ActionResult> BulkDeleteEntity<T>() 
            where T:class
        {
            string ids = FormValue<string>("ids");
            if (string.IsNullOrWhiteSpace(ids))
                return JsonError("Invalid Request");
            string query = "{ '$id:in':[" + ids + "]}";
            var result = await db.Query<T>().WhereJsonQuery(query, db.SecurityContext).ToListAsync();

            foreach (var e in result) {
                db.DeleteEntity(e);
            }
            await db.SaveAsync();

            return JsonResult("");            
        }

        

        public virtual async Task<ActionResult> DeleteEntity<T>()
            where T : class
        {
            if (!Request.IsAjaxRequest())
            {
                if (!Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonError("POST Method Required");
                }
            }

            T item = Activator.CreateInstance<T>();
            LoadModel(item);

            Type type = typeof(T);

            item = await db.Query<T>().WhereCopy(item).FirstOrDefaultAsync();
            if (item == null) {
                return JsonError("Failed to Load Entity. " + type.FullName);
            }

            item = (T)db.DeleteEntity(item);
            await db.SaveAsync();

            T retItem = item;
            return JsonResult(retItem);
        }

        public virtual async Task<ActionResult> MoveEntity<T>(
            string query,
            string direction = "up",
            string orderBy = "SortOrder"
            )
            where T : class
        {


            if (!Request.IsAjaxRequest())
            {
                if (!Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonError("POST Method Required");
                }
            }


            T item = Activator.CreateInstance<T>();
            LoadModel(item);

            var aq = db.Query<T>().WhereJsonQuery(query,db.SecurityContext);
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return JsonError("orderBy missing");
            }
            var list = await aq.OrderBy(orderBy + " ASC").ToListAsync();


            T srcItem = await db.Query<T>().WhereCopy(item).FirstOrDefaultAsync();
                
            int index = list.IndexOf(srcItem);
            list.Remove(srcItem);

            bool up = string.IsNullOrWhiteSpace(direction) ? true : string.Equals(direction,"up", StringComparison.OrdinalIgnoreCase);

            if (up)
            {
                index = index - 1;
            }
            else
            {
                index = index + 1;
            }

            list.Insert(index, srcItem);



            int i = 1;

            PropertyInfo p = typeof(T).GetProperty(orderBy);

            foreach (var listItem in list)
            {
                p.SetValue(listItem, i, null);
                i++;
            }

            await Repository.SaveAsync();

            return JsonResult(srcItem);
        }

        public virtual async Task<ActionResult> BulkSaveEntity<T>() 
            where T:class
        {

            string ids = FormValue<string>("ids");

            string query = "{'$id:in':['" + ids + "']}";

            var list = await db.Query<T>().WhereJsonQuery(query, db.SecurityContext).ToListAsync();
            foreach (var item in list)
            {
                LoadModel(item);
            }
            await db.SaveAsync();
            return JsonResult("");
        }

        public virtual async Task<ActionResult> SaveEntity<T>()
            where T : class
        {
            if (!Request.IsAjaxRequest())
            {
                if (!Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonError("POST Method Required");
                }
            }

            Type type = typeof(T);

            T item = Activator.CreateInstance<T>();

            var key = type.GetEntityProperties(true).FirstOrDefault();

            LoadModel(item);

            T copy = await db.Query<T>().WhereCopy(item).FirstOrDefaultAsync();
            if (copy != null)
            {
                // merge...

                LoadModel(copy);
                item = copy;

            }
            else
            {
                Repository.AddEntity(item);


            }

            await db.SaveAsync();

            return JsonResult(item);
        }

        //protected virtual void OnInserting<T>(T item)
        //{
        //    Type type = typeof(T);
        //    foreach (PropertyInfo p in type.GetNavigationProperties()) { 
        //        object val = null;
        //        if (this.FormModel.TryGetValue(p.Name, out val))
        //        {
        //            Type entityType = p.PropertyType.GetGenericArguments().FirstOrDefault();
        //            dynamic coll = p.GetValue(item,null);
        //            // should be an array...
        //            foreach (var child in (System.Collections.IEnumerable)val)
        //            {
        //                if (child == null)
        //                    continue;
        //                object e = Activator.CreateInstance(entityType);

        //                object copy = ObjectContext.QuerySourceEntity(e).FirstOrDefault();
        //                if (copy == null)
        //                    copy = e;
        //                LoadModel(copy, child as Dictionary<string, object>);

        //                coll.Add(copy);
        //            }
        //        }
        //    }
        //}

        private static ThreadSafeDictionary<string, object> Methods = new ThreadSafeDictionary<string, object>();

        //protected void InvokeMethod<T>(string methodName, T item, ObjectStateEntry entry = null) {
        //    Type type = typeof(T);
        //    string key = type.FullName + ":" + methodName;
        //    if (entry == null)
        //    {
        //        Action<object, T> action = (Action<object, T>)Methods.GetOrAdd(key, k => GetMethodSignature(methodName, type));
        //        if (action != null)
        //        {
        //            action(this,item);
        //        }
        //    }
        //    else {
        //        Action<object,T, ObjectStateEntry> action = (Action<object,T, ObjectStateEntry>)Methods.GetOrAdd(key, k => GetMethodSignature(methodName, type, typeof(ObjectStateEntry)));
        //        if (action != null)
        //        {
        //            action(this,item, entry);
        //        }
        //    }
        //}

        private object GetMethodSignature(string methodName, params Type[] types)
        {

            List<ParameterExpression> plist = new List<ParameterExpression>();
            plist.Add(Expression.Parameter(typeof(object)));
            foreach (var item in types)
            {
                plist.Add(Expression.Parameter(item));
            }

            MethodInfo m = GetType().GetMethod(methodName, types);
            if (m == null)
                return null;

            Expression exp = Expression.Call( Expression.TypeAs(plist.First(),GetType()), m, plist.Skip(1).ToArray());

            return Expression.Lambda(exp,plist).Compile();
        }

        public async Task<ActionResult> Query(string table, string query, string fields, string orderBy, int start = 0, int size = 10)
        {

            Type type = GetType(this.Repository, table);

            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "QueryEntity", type, query, fields, orderBy, start, size);
        }

        public async Task<ActionResult> Move(string table, string query, string orderBy, string direction)
        {

            Type type = GetType(this.Repository, table);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "MoveEntity", type, query, direction, orderBy);

        }

        public async Task<ActionResult> Get(string table, string query, string fields,string orderBy)
        {
            Type type = GetType(this.Repository, table);

            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "GetEntity", type, query, fields, orderBy);
        }

        public async Task<ActionResult> BulkSave(string table) {
            Type type = GetType(this.Repository, table);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "BulkSaveEntity", type);
        }

        public async Task<ActionResult> Save(string table)
        {
            Type type = GetType(this.Repository, table);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "SaveEntity", type);
        }

        public async Task<ActionResult> BulkDelete(string table)
        {
            Type type = GetType(this.Repository, table);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "BulkDeleteEntity", type);
        }

        public async Task<ActionResult> Delete(string table)
        {
            Type type = GetType(this.Repository, table);
            return await (Task<ActionResult>)GenericMethods.InvokeGeneric(this, "DeleteEntity", type);
        }

        private static ThreadSafeDictionary<string, Type> typeCache = new ThreadSafeDictionary<string, Type>();

        protected Type GetType(TOC db, string table)
        {
            table = table.Replace("-","");

            string key = db.GetType().FullName + ":" + table;

            return typeCache.GetOrAdd(key, f =>
            {

                Type t = db.GetType();
                foreach (var item in t.GetProperties())
                {
                    Type g = item.PropertyType.GetGenericArguments().FirstOrDefault();
                    if (g == null)
                        continue;
                    if (string.Equals(g.Name, table, StringComparison.InvariantCultureIgnoreCase))
                    {
                        typeCache[key] = g;
                        return g;
                    }
                }

                throw new KeyNotFoundException("Table " + table + " not found");
            });
        }


        private Action<Associations, int, object> CreateRelatedLoadExpression(Type type, string[] propList)
        {
            ParameterExpression plist = Expression.Parameter(typeof(Associations), "list");
            ParameterExpression pindex = Expression.Parameter(typeof(int), "index");
            ParameterExpression psrc = Expression.Parameter(typeof(object), "src");

            ParameterExpression pvar = Expression.Variable(typeof(object), "v");

            List<Expression> statementList = new List<Expression>();

            MethodInfo m = typeof(Associations).GetMethod("AddEntry");

            foreach (var item in propList)
            {
                PropertyInfo p = type.GetProperty(item);

                var propExp = Expression.Property(Expression.TypeAs(psrc, type), p);

                statementList.Add(Expression.Call(plist, m, new Expression[] { pindex, Expression.Constant(item), propExp }));

            }

            Expression body = Expression.Block(statementList.ToArray());

            return Expression.Lambda<Action<Associations, int, object>>(body, plist, pindex, psrc).Compile();
        }


        public class AssociationMap
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class RelatedEntity
        {

            public RelatedEntity()
            {
                map = new List<AssociationMap>();
            }

            public object entity { get; set; }

            public string type { get; set; }

            public List<AssociationMap> map { get; private set; }
        }

        public class Associations : List<RelatedEntity>
        {

            public void AddEntry(int i, string name, object entity)
            {
                if (entity == null)
                    return;
                var entry = this.FirstOrDefault(x => x.entity == entity);
                if (entry == null)
                {
                    entry = new RelatedEntity
                    {
                        entity = entity,
                        type = entity.GetType().Name
                    };
                    this.Add(entry);
                }
                entry.map.Add(new AssociationMap { id = i, name = name });
            }

        }

        private static ThreadSafeDictionary<string, Action<Associations, int, object>> relatedExpressions
            = new ThreadSafeDictionary<string, Action<Associations, int, object>>();


        [NonAction]
        public IQueryable GetEntityQueryByKey<T>(object key)
            where T:class
        {
            return db.Query<T>().WhereKey(key);
        }

        [HttpPost]
        public async Task<ActionResult> SaveChanges()
        {
            ChangeSet model = new ChangeSet();
            LoadModel(model);

            List<object> changes = new List<object>();

            foreach (Change entity in model.entities)
            {
                string entityType = entity.type;
                Type clrType = GetType(Repository, entityType);
                object dbEntity;
                object id = entity.id;
                if (id == null)
                {
                    dbEntity = Activator.CreateInstance(clrType);
                    LoadModel(dbEntity, entity.changes);
                    db.AddEntity(dbEntity);
                    changes.Add(dbEntity);
                }
                else
                {
                    IQueryable qe = (IQueryable)GenericMethods.InvokeGeneric(this, "GetEntityQueryByKey", clrType, id);
                    object final = (await qe.ToListAsync()).FirstOrDefault();
                    if (final == null)
                    {
                        continue;
                    }
                    dbEntity = final;
                    LoadModel(dbEntity, entity.changes);
                    changes.Add(dbEntity);
                }

            }

            foreach (Association association in model.associations)
            {
                int id = association.id;

                object parent = changes[id];

                Type parentType = parent.GetType();

                foreach (var item in association.added)
                {
                    int cid = item.id;
                    dynamic child = changes[cid];
                    PropertyInfo p = parentType.GetProperty(item.name);
                    System.Collections.IEnumerable list = p.GetValue(parent) as System.Collections.IEnumerable;
                    if (list == null)
                    {
                        p.SetValue(parent, child);
                    }
                    else
                    {
                        ((dynamic)list).Add(child);
                    }
                }
                foreach (var item in association.removed)
                {
                    int cid = item.id;
                    dynamic child = changes[cid];
                    PropertyInfo p = parentType.GetProperty(item.name);
                    dynamic d = p.GetValue(parent);
                    //d.Load();
                    d.Remove(child);
                    var da = p.GetCustomAttribute<DeleteEntityOnRemoveAttribute>(true);
                    if (da != null)
                    {
                        object co = child;
                        Repository.DeleteEntity(co);
                    }
                }
            }



            await db.SaveAsync();

            return JsonResult(changes);
        }


        public abstract class ListLoader : IJavaScriptDeserializable
        {
            public void Deserialize(Dictionary<string, object> values)
            {
                Type type = GetType();
                foreach (KeyValuePair<string, object> item in values)
                {
                    PropertyInfo p = type.GetProperty(item.Key);
                    if (p == null)
                        continue;
                    if (item.Value == null)
                        continue;
                    Type pt = p.PropertyType;
                    if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        dynamic list = p.GetValue(this);
                        if (list == null)
                            continue;
                        Type listItemType = pt.GetGenericArguments()[0];
                        foreach (object value in (System.Collections.IEnumerable)item.Value)
                        {
                            ListLoader listItem = Activator.CreateInstance(listItemType) as ListLoader;
                            if (listItem == null)
                                continue;
                            list.Add((dynamic)listItem);
                            listItem.Deserialize((Dictionary<string, object>)value);
                        }
                    }
                    else
                    {
                        object value = item.Value;
                        if (pt != value.GetType())
                        {
                            value = Convert.ChangeType(value, pt);
                        }
                        p.SetValue(this, value);
                    }
                }
            }
        }

        public class ChangeSet : ListLoader
        {

            public ChangeSet()
            {
                entities = new List<Change>();
                associations = new List<Association>();
            }

            public List<Change> entities { get; set; }
            public List<Association> associations { get; set; }

        }

        public class Change : ListLoader
        {
            public object id { get; set; }
            public string type { get; set; }
            public Dictionary<string, object> changes { get; set; }

        }

        public class Association : ListLoader
        {

            public Association()
            {
                added = new List<AssociationEdit>();
                removed = new List<AssociationEdit>();
            }

            public int id { get; set; }
            public List<AssociationEdit> added { get; set; }
            public List<AssociationEdit> removed { get; set; }

        }

        public class AssociationEdit : ListLoader
        {
            public int id { get; set; }
            public string name { get; set; }

        }
 

    }
}
