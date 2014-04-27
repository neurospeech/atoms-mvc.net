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
//    public class EntityProperty
//    {
//        public EntityProperty(PropertyInfo property)
//        {
//            Property = property;
//            Name = property.Name;
//            List<FKPropertyAttribute> list = new List<FKPropertyAttribute>();
//            Object[] attrs = property.GetCustomAttributes(typeof(FKPropertyAttribute), true);
//            if (attrs != null && attrs.Length > 0)
//            {
//                foreach (FKPropertyAttribute at in attrs)
//                {
//                    list.Add(at);
//                }
//            }
//            FKProperties = list.ToArray();

//            attrs = Property.GetCustomAttributes(typeof(EdmScalarPropertyAttribute), true);
//            if (attrs != null && attrs.Length > 0)
//            {
//                EdmScalarPropertyAttribute a = attrs[0] as EdmScalarPropertyAttribute;
//                IsKey = a.EntityKeyProperty;
//            }
//        }
//        public string Name { get; set; }
//        public bool IsKey { get; set; }
//        public PropertyInfo Property { get; set; }
//        public FKPropertyAttribute[] FKProperties { get; set; }

//        public string GetValue(object obj)
//        {
//            if (obj == null)
//                return null;
//            object v = Property.GetValue(obj, null);
//            if (v == null)
//                return null;
//            return v.ToString();
//        }
//    }
//}
