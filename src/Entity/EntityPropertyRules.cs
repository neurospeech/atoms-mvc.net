using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity
{
    public class EntityPropertyRules
    {
        public object ReadRule { get; set; }

        public object WriteRule { get; set; }

        public object DeleteRule { get; set; }

        public string TypeName { get; private set; }

        public string FullTypeName { get; private set; }

        private ThreadSafeDictionary<string, SerializeMode> Rules { get; set; }

        public EntityPropertyRules(string typeName, string fullTypeName)
        {
            TypeName = typeName;
            FullTypeName = fullTypeName;
            Rules = new ThreadSafeDictionary<string, SerializeMode>();
        }

        public void SetMode(string propertyName, SerializeMode mode) {
            this[propertyName] = mode;
        }

        public SerializeMode this[string propertyName]
        {
            get
            {
                SerializeMode m = SerializeMode.None;
                if(Rules.TryGetValue(propertyName, out m))
                    return m;
                return SerializeMode.None;
            }
            set
            {
                Rules[propertyName] = value;
            }
        }

        List<PropertyDescriptor> cache = null;

        public PropertyDescriptorCollection GetProperties(object owner, Type type = null) {
            lock (this)
            {
                if (cache == null)
                {
                    cache = new List<PropertyDescriptor>();

                    PropertyDescriptorCollection pdc = owner == null ? TypeDescriptor.GetProperties(type) : TypeDescriptor.GetProperties(owner, true);

                    foreach (PropertyDescriptor item in pdc)
                    {
                        SerializeMode mode = this[item.Name];
                        switch (mode)
                        {
                            case SerializeMode.ReadWrite:
                                cache.Add(new AtomPropertyDescriptor(item, item.Name, null));
                                break;
                            //case SerializeMode.Receive:
                            //    cache.Add(new AtomPropertyDescriptor(item, item.Name, null, new XmlIgnoreAttribute(), new ScriptIgnoreAttribute()));
                            //    break;
                            case SerializeMode.Read:
                                cache.Add(new AtomPropertyDescriptor(item, item.Name, true));
                                break;
                            case SerializeMode.None:
                            case SerializeMode.Calculate:
                                cache.Add(new AtomPropertyDescriptor(item, item.Name, true, new XmlIgnoreAttribute(), new ScriptIgnoreAttribute()));
                                break;
                            case SerializeMode.Default:
                                cache.Add(item);
                                break;
                        }
                    }
                }
                return new PropertyDescriptorCollection(cache.ToArray());
            }
        }

        public EntityPropertyRules Clone()
        {
            EntityPropertyRules copy = new EntityPropertyRules(this.TypeName, this.FullTypeName);
            foreach (var item in Rules)
            {
                copy[item.Key] = item.Value;
            }
            return copy;
        }

        public IEnumerable<KeyValuePair<string, SerializeMode>> PublicProperties {
            get {
                return Rules.Where(x => x.Value != SerializeMode.None);
            }
        }
    }

}
