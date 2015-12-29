using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity.Audit
{
    public class FKPropertyAttribute : Attribute
    {

        public FKPropertyAttribute(string e, string p)
        {
            EntityName = e;
            PropertyName = p;

            
        }

        public string EntityName { get; set; }

        public string PropertyName { get; set; }

    }
}
