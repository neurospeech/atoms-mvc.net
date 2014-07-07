using NeuroSpeech.Atoms.Entity.Audit;
using NeuroSpeech.Atoms.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{
    public class ChangeSet
    {

        public class ChangeEntry {

            public ChangeEntry(ObjectStateEntry f)
            {
                Entity = f.Entity;
                State = f.State;
                if (State != EntityState.Modified)
                    return;
                
                var d = new Dictionary<string, object>();
                foreach (var item in f.GetModifiedProperties())
                {
                    d[item] = f.OriginalValues[item];
                }
                OriginalValues = d;
                
            }

            public ChangeEntry(System.Data.Entity.Infrastructure.DbEntityEntry f)
            {
                Entity = f.Entity;
                State = f.State;
                if (State != EntityState.Modified)
                    return;

                var d = new Dictionary<string, object>();
                foreach (var item in f.OriginalValues.PropertyNames)
                {
                    var fp = f.Property(item);
                    if (!fp.IsModified)
                        continue;
                    d[item] = fp.CurrentValue;
                }
            }

            public object Entity { get; set; }
            public EntityState State { get; set; }
            public Dictionary<string,object> OriginalValues { get; set; }
        }

        public IEnumerable<ChangeEntry> Added { get; private set; }
        public IEnumerable<ChangeEntry> Modified { get; private set; }
        public IEnumerable<ChangeEntry> Deleted { get; private set; }

        public IEnumerable<ChangeEntry> UpdatedEntities
        {
            get
            {
                foreach (var item in this.Added)
                {
                    yield return item;
                }
                foreach (var item in this.Modified)
                {
                    yield return item;
                }
            }
        }

        public ChangeSet(ObjectContext oc)
        {
            Added = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Select(f=> new ChangeEntry(f)).ToList();
            Deleted = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Deleted).Select(f => new ChangeEntry(f)).ToList();
            Modified = oc.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Select(f => new ChangeEntry(f)).ToList();

        }

        public ChangeSet(DbContext dc)
        {
            Added = dc.ChangeTracker.Entries().Where(x=>x.State == EntityState.Added).Select(f=> new ChangeEntry(f)).ToList();
            Deleted = dc.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Select(f => new ChangeEntry(f)).ToList();
            Modified = dc.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).Select(f => new ChangeEntry(f)).ToList();
        }

        public void BeginAudit()
        {
            Changes = new List<Change>();
            // add everything what was added first...
            foreach (var item in Added)
            {
                var c = new Change(item.Entity, item.State,null);
                Changes.Add(c);
            }
            foreach (var item in Modified)
            {
                var c = new Change(item.Entity, item.State, item.OriginalValues);
                Changes.Add(c);
            }
            foreach (var item in Deleted)
            {
                var c = new Change(item.Entity, item.State,null);
                Changes.Add(c);
            }
        }


        public List<Change> Changes { get; private set; }


        internal void EndAudit(IAuditContext ac)
        {
            PrepareChanges(ac);

            ac.SaveChanges();
        }

        internal async Task<int> EndAuditAsync(IAuditContext ac)
        {
            PrepareChanges(ac);

            return await ac.SaveChangesAsync();
        }

        private void PrepareChanges(IAuditContext ac)
        {
            foreach (var item in Changes.Where(x => x.State == EntityState.Added))
            {
                Type type = item.Entity.GetType();
                List<string> keyValues = new List<string>();
                foreach (var k in type.GetEntityProperties(true))
                {
                    var key = k.GetValue(item.Entity);

                    var pv = item.Values.FirstOrDefault(x => x.Name == k.Name);
                    if (pv == null)
                    {
                        pv = new CFieldValue { Name = k.Name };
                        item.Values.Add(pv);
                    }
                    pv.NewValue = key;

                    keyValues.Add(key.ToString());
                }

                item.Key = string.Join(",", keyValues);
            }

            List<CLinkValue> links = new List<CLinkValue>();
            foreach (var change in Changes)
            {
                foreach (var item in change.Values)
                {
                    foreach (var fk in item.Property.FKProperties)
                    {
                        CLinkValue v = new CLinkValue();
                        v.ObjectName = fk.EntityName;
                        var v1 = item.NewValue ?? item.OldValue;
                        if (v1 == null)
                            continue;
                        v.Key = v1.ToString();
                        if (string.IsNullOrWhiteSpace(v.Key))
                            continue;
                        v.ChildObject = item.Property.Property.DeclaringType.Name;
                        v.ChildKey = change.Key;
                        v.Operation = change.State.ToString();
                        links.Add(v);
                    }
                }
            }

            foreach (var item in links.GroupBy(x => x.ObjectName))
            {
                string name = item.Key;
                foreach (var k in item.GroupBy(x => x.Key).ToList())
                {
                    string key = k.Key;
                    Change c = Changes.FirstOrDefault(x => x.ObjectName == name && x.Key == key);
                    if (c != null)
                    {
                        c.Links.AddRange(k);
                    }
                    else
                    {
                        c = new Change(EntityState.Modified);
                        c.ObjectName = name;
                        c.Key = key;
                        c.Links.AddRange(k);
                        Changes.Add(c);
                    }
                }
            }


            JavaScriptSerializer sr = new JavaScriptSerializer();

            foreach (var change in Changes)
            {
                if (change.Entity is IAuditIgnore)
                    continue;
                IAuditItem item = ac.CreateNew();
                item.Action = change.State.ToString();
                item.AuditTime = DateTime.UtcNow;
                item.IPAddress = ac.GetIPAddress();
                item.Username = ac.GetUsername();
                item.TableKey = change.Key;
                item.TableName = change.ObjectName;
                item.Fields = sr.Serialize(change.Values);
                item.Links = sr.Serialize(change.Links);
                ac.AddAudit(item);
            }
        }
    }


    public class Change {


        [XmlIgnore]
        [ScriptIgnore]
        internal object Entity;

        public string ObjectName { get; set; }
        public string Key { get; set; }
        public EntityState State { get; set; }

        public List<CFieldValue> Values { get; private set; }
        public List<CLinkValue> Links { get; private set; }

        public Change(EntityState state)
        {
            State = state;
            Links = new List<CLinkValue>();
        }


        public Change(object entity, EntityState state , Dictionary<string,object> originalValues)
        {
            this.Entity = entity;

            Values = new List<CFieldValue>();
            Links = new List<CLinkValue>();

            Type type = entity.GetType();

            this.State = state;
            this.ObjectName = type.Name;

            if (state != EntityState.Added)
            {
                List<string> keyValues = new List<string>();
                foreach (var p in type.GetEntityProperties(true))
                {
                    keyValues.Add(p.GetValue(entity).ToString());
                }
                this.Key = string.Join("," , keyValues);
            }

            foreach (var p in type.GetEntityProperties())
            {
                var pv = new CFieldValue {
                    Name = p.Name,
                    Property = p
                };
                if (state == EntityState.Added)
                {
                    pv.NewValue = p.GetValue(entity);
                }
                else {
                    pv.NewValue = p.GetValue(entity);
                    if (originalValues != null) {
                        if (originalValues.ContainsKey(p.Name))
                        {
                            pv.OldValue = originalValues[p.Name];
                        }
                        else {
                            continue;
                        }
                    }
                }
                Values.Add(pv);
            }



        }

    }

    public class CFieldValue {
        public string Name { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        [ScriptIgnore]
        [XmlIgnore]
        internal AtomPropertyInfo Property { get; set; }

    }

    public class CLinkValue {
        public string ObjectName { get; set; }
        public string Key { get; set; }
        public string ChildObject { get; set; }
        public string ChildKey { get; set; }
        public string Operation { get; set; }
        public string ChildProperty { get; set; }
    }


}
