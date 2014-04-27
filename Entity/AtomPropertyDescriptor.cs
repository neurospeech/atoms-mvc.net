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
    public class AtomPropertyDescriptor : PropertyDescriptor
    {
        PropertyDescriptor desc;

        bool? readOnly = null;

        public AtomPropertyDescriptor(PropertyDescriptor pd, string name, bool? readOnly, params Attribute[] attrs) :
            base(name, attrs)
        {
            desc = pd;
            this.readOnly = readOnly;
        }

        public override bool CanResetValue(object component)
        {
            return desc.CanResetValue(component);
        }

        public override Type ComponentType
        {
            get
            {
                return desc.ComponentType;
            }
        }

        public override object GetValue(object component)
        {
            return desc.GetValue(component);
        }

        public override bool IsReadOnly
        {
            get
            {
                if (readOnly.HasValue)
                    return readOnly.Value;
                return desc.IsReadOnly;
            }
        }

        public override Type PropertyType
        {
            get { return desc.PropertyType; }
        }

        public override void ResetValue(object component)
        {
            desc.ResetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            desc.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return desc.ShouldSerializeValue(component);
        }
    }
}
