//using System;
//using System.Collections.Generic;
//using System.Data.Objects;
//using System.Data.Objects.DataClasses;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Xml.Serialization;

//namespace NeuroSpeech.Atoms.Entity.Audit
//{
//    internal class Change
//    {
//        internal object NewValue;
//        internal object OldValue;
//        internal object Entity;
//        internal string TableName;
//        internal string TableKey;
//        internal List<FieldValue> Fields;
//        internal List<LinkValue> Links;
//        public System.Data.EntityState State;

//        internal Change()
//        {
//            Fields = new List<FieldValue>();
//            Links = new List<LinkValue>();
//        }

//        internal void Build()
//        {
//            object latest = NewValue ?? OldValue;

//            List<EntityProperty> props = GetProperties(NewValue ?? OldValue);
//            foreach (EntityProperty item in props)
//            {
//                FieldValue cv = new FieldValue(item);
//                cv.Name = item.Name;

//                cv.NewValue = item.GetValue(NewValue);
//                if (OldValue != null)
//                {
//                    cv.OldValue = item.GetValue(OldValue);
//                    if ((cv.OldValue != null && cv.NewValue != null && cv.OldValue.Equals(cv.NewValue)))
//                        continue;
//                }
//                if (cv.OldValue != null || cv.NewValue != null)
//                {
//                    Fields.Add(cv);
//                }
//            }

//            List<string> KeyValues = new List<string>();
//            foreach (var item in props.Where(x => x.IsKey))
//            {
//                KeyValues.Add(item.GetValue(latest));
//            }
//            this.TableKey = string.Join(",", KeyValues);

//            foreach (var item in props)
//            {
//                foreach (var fk in item.FKProperties)
//                {
//                    LinkValue v = new LinkValue();
//                    v.ObjectName = fk.EntityName;
//                    v.Key = item.GetValue(latest);
//                    if (string.IsNullOrWhiteSpace(v.Key))
//                        continue;
//                    v.ChildObject = item.Property.DeclaringType.Name;
//                    v.ChildKey = TableKey;
//                    v.Operation = State.ToString();
//                    Links.Add(v);
//                }
//            }
//        }

//        private List<EntityProperty> GetProperties(object p)
//        {
//            List<EntityProperty> list = new List<EntityProperty>();
//            foreach (var item in p.GetType().GetProperties())
//            {
//                if (!(item.CanRead && item.CanWrite))
//                    continue;
//                Type t = item.PropertyType;
//                if (t == typeof(string) || t.IsValueType)
//                    list.Add(new EntityProperty(item));
//            }
//            return list;
//        }
//    }
//}
