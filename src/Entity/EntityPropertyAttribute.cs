using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityPropertyAttribute : Attribute
    {

        public bool IsKey { get; set; }

        public EntityPropertyAttribute()
        {

        }

        public EntityPropertyAttribute(bool key)
        {
            IsKey = key;
        }

    }
}
