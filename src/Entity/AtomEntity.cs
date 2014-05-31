using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{
    public class AtomEntity :
            EntityObject,
            IJavaScriptDeserializable,
            System.ComponentModel.ICustomTypeDescriptor
    {


        //string IJavaScriptSerializable.Serialize(bool json)
        //{
        //    AtomJavaScriptSerializer js = new AtomJavaScriptSerializer(json);
        //    List<string> properties = new List<string>();

        //    foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(this))
        //    {
        //        if (pd.Attributes.OfType<ScriptIgnoreAttribute>().Any())
        //            continue;
        //        if (pd.Attributes.OfType<XmlIgnoreAttribute>().Any())
        //            continue;
        //        object value = pd.GetValue(this);
        //        string pName = pd.Name;
        //        if (json)
        //        {
        //            pName = "\"" + pName + "\"";
        //        }
        //        properties.Add(pName + ":" + js.Serialize(value));
        //    }

        //    return "{" + string.Join(",", properties) + "}";
        //}

        void IJavaScriptDeserializable.Deserialize(System.Collections.Generic.Dictionary<string, object> values)
        {
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(this))
            {
                if (pd.IsReadOnly)
                    continue;
                object val;
                if (!values.TryGetValue(pd.Name, out val))
                    continue;
                if (val != null)
                {
                    Type pt = pd.PropertyType;
                    if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(System.Nullable<>))
                    {
                        pt = pt.GetGenericArguments()[0];
                    }
                    if (pt == typeof(DateTime) && val.GetType() != typeof(DateTime))
                    {

                        string dt = val.ToString();
                        if (dt.StartsWith("/Date"))
                        {
                            val = AtomJavaScriptSerializer.ToDateTime(dt);
                        }
                        else
                        {
                            // parse the UTC time 
                            try
                            {
                                val = DateTime.Parse(dt);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException("Parsing failed for " + pd.Name + " with " + dt, ex);
                            }
                        }
                    }
                    if (pt == typeof(Guid))
                    {
                        val = Guid.Parse((string)val);
                    }
                    if (val is IDictionary<string, object>)
                    {
                        continue;
                    }


                    try
                    {

                        if (val == null)
                        {
                            if (pt.IsValueType)
                            {
                                val = 0;
                            }
                        }
                        else {
                            string v = (val as string) ?? val.ToString();
                            if (string.IsNullOrWhiteSpace(v) && pt.IsValueType) {
                                val = 0;
                            }
                        }
                        val = Convert.ChangeType(val, pt);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("ChangeType failed for " + pd.Name + " with " + val, ex);
                    }
                }

                object oldValue = pd.GetValue(this);
                if (oldValue == val)
                    continue;
                if (oldValue != null && val != null && val.Equals(oldValue))
                    continue;
                pd.SetValue(this, val);
            }
        }

        System.ComponentModel.AttributeCollection System.ComponentModel.ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        string System.ComponentModel.ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        string System.ComponentModel.ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        System.ComponentModel.TypeConverter System.ComponentModel.ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        System.ComponentModel.EventDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        System.ComponentModel.PropertyDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object System.ComponentModel.ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(this, attributes, true);
        }

        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties()
        {
            if (_NetworkContext == null)
            {
                if (ObjectContext == null || ObjectContext.SecurityContext == null)
                    return TypeDescriptor.GetProperties(this, true);
                return ObjectContext.SecurityContext[this.GetType()].GetProperties(this);
            }
            return _NetworkContext.GetProperties(this);
        }

        object System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd)
        {
            return this;
        }

        [XmlIgnore]
        [ScriptIgnore]
        public EntityPropertyRules NetworkContext
        {
            get
            {
                return _NetworkContext ?? (_NetworkContext = CreateNetworkContext(ObjectContext));
            }
        }

        [XmlIgnore]
        [ScriptIgnore]
        internal EntityPropertyRules SecurityRules {
            get {
                return _NetworkContext;
            }
        }

        private EntityPropertyRules CreateNetworkContext(ISecureRepository ObjectContext)
        {
            if (ObjectContext == null)
                throw new InvalidOperationException("Object Context is not set");
            var sc = ObjectContext.SecurityContext;
            if (sc == null)
                return BaseSecurityContext.DefaultEntityPropertyRules(this.GetType());
            return sc[this.GetType()].Clone();
        }

        [XmlIgnore]
        [ScriptIgnore]
        public ISecureRepository ObjectContext
        {
            get;
            set;
        }

        private EntityPropertyRules _NetworkContext;

        public AtomEntity()
        {
        }
    }
}
