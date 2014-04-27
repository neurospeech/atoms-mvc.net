//using NeuroSpeech.Atoms.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Data.Objects;
//using System.Data.Objects.DataClasses;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Web.Mvc;
//using System.Web.Script.Serialization;
//using System.Xml.Serialization;

//namespace NeuroSpeech.Atoms.Entity.Audit
//{

//    public static class AuditHelper
//    {
//        public static int AuditChanges(this AtomObjectContext context, IAuditContext auditContext, Func<int> func)
//        {
//            using (AtomObjectContext copy = Activator.CreateInstance(context.GetType()) as AtomObjectContext)
//            {
//                List<Change> changes = new List<Change>();

//                var addedEntries = GetEntries(context, copy, System.Data.EntityState.Added);
//                var removedEntries = GetEntries(context, copy, System.Data.EntityState.Deleted);
//                var modifiedEntries = GetEntries(context, copy, System.Data.EntityState.Modified);
//                changes.AddRange(addedEntries);
//                changes.AddRange(removedEntries);
//                changes.AddRange(modifiedEntries);
//                int retval = func();
//                SaveEntries(changes, auditContext);
//                return retval;
//            }
//        }

//        internal static List<Change> GetEntries(this AtomObjectContext context, AtomObjectContext copy, System.Data.EntityState state)
//        {
//            List<Change> changes = new List<Change>();
//            foreach (var item in context.ObjectStateManager.GetObjectStateEntries(state))
//            {
//                if (item.Entity is IAuditIgnore)
//                    continue;

//                Change c = new Change();
//                c.Entity = item.Entity;
//                c.State = state;
//                switch (state)
//                {
//                    case System.Data.EntityState.Added:
//                        c.NewValue = item.Entity;
//                        c.TableName = item.Entity.GetType().Name;
//                        break;
//                    case System.Data.EntityState.Deleted:
//                        c.OldValue = item.Entity;
//                        c.TableName = item.Entity.GetType().Name;
//                        break;
//                    case System.Data.EntityState.Detached:
//                        break;
//                    case System.Data.EntityState.Modified:
//                        c.NewValue = item.Entity;
//                        c.OldValue = GenericMethods.InvokeGeneric(copy,"LoadEntity", item.Entity.GetType(),item.Entity);
//                        c.TableName = item.Entity.GetType().Name;
//                        break;
//                    case System.Data.EntityState.Unchanged:
//                        break;
//                    default:
//                        break;
//                }
//                changes.Add(c);
//            }
//            return changes;
//        }


//        private static void SaveEntries(List<Change> entries, IAuditContext auditContext)
//        {
//            List<LinkValue> links = new List<LinkValue>();
//            foreach (var item in entries)
//            {
//                item.Build();
//                links.AddRange(item.Links);
//                item.Links.Clear();
//            }

//            foreach (var item in links.GroupBy(x => x.ObjectName).ToList())
//            {
//                string name = item.Key;
//                foreach (var k in item.GroupBy(x => x.Key).ToList())
//                {
//                    string key = k.Key;
//                    Change c = entries.FirstOrDefault(x => x.TableName == name && x.TableKey == key);
//                    if (c != null)
//                    {
//                        c.Links.AddRange(k);
//                    }
//                    else
//                    {
//                        c = new Change();
//                        c.State = System.Data.EntityState.Modified;
//                        c.TableName = name;
//                        c.TableKey = key;
//                        c.Links.AddRange(k);
//                        entries.Add(c);
//                    }
//                }
//            }

//            IAuditContext auditEntities = auditContext;
//            foreach (var item in entries)
//            {
//                IAuditItem a = auditEntities.CreateNew();
//                a.AuditTime = DateTime.Now;
//                a.IPAddress = auditEntities.GetIPAddress();
//                a.Username = auditEntities.GetUsername();
//                a.TableName = item.TableName;
//                a.Action = item.State.ToString();
//                a.TableKey = item.TableKey;

//                if (item.Fields.Count > 0)
//                    a.Fields = ToJSON(item.Fields.ToList());
//                if (item.Links.Count > 0)
//                    a.Links = ToJSON(item.Links.ToList());

//                auditEntities.AddAudit(a);
//            }
//            auditEntities.SaveChanges();
//        }

//        private static string ToJSON(object p)
//        {
//            if (p == null)
//                return null;
//            string v = p as string;
//            if (v != null)
//                return v;
//            /*XmlSerializer xs = new XmlSerializer(p.GetType());
//            StringWriter sw = new StringWriter();
//            xs.Serialize(sw, p);
//            return sw.ToString();*/
//            AtomJavaScriptSerializer jsr = new AtomJavaScriptSerializer(null);
//            return jsr.Serialize(p);
//        }
//    }
//}
