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

        public Type Type { get; private set; }

        private ThreadSafeDictionary<string, SerializeMode> Rules { get; set; }

        public EntityPropertyRules(Type type)
        {
            TypeName = type.Name;
            Type = type;
            Rules = new ThreadSafeDictionary<string, SerializeMode>();
        }

        public void SetMode(string propertyName, SerializeMode mode) {
            Rules[propertyName] = mode;
        }

        public SerializeMode this[string name]
        {
            get {
                SerializeMode n = SerializeMode.None;
                if(Rules.TryGetValue(name,out n))
                    return n;
                return SerializeMode.None;
            }
        }

        public IEnumerable<string> PublicProperties {
            get {
                return Rules.Where(x => x.Value != SerializeMode.None).Select(x=>x.Key);
            }
        }
    }

}
