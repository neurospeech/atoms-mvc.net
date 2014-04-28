using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using NeuroSpeech.Atoms.Entity;
using System.Collections.Concurrent;
using System.Xml.Serialization;
using System.Diagnostics;

namespace NeuroSpeech.Atoms.Linq {


    public interface IEntityWrapper { 
    }

    public class LinqField
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public Type OwnerType { get; set; }

        public PropertyInfo Property { get; set; }

        public string PropertyPath { get; set; }

        public Expression Expression { get; set; }

        public bool Protected { get; set; }
   }


    public class LinqFields : List<LinqField>
    {
        public LinqFields()
        {

        }

        public LinqFields(Dictionary<string,PropertyInfo> fields)
        {
            foreach (var item in fields.OrderBy(x=>x.Key))
            {
                this.Add(new LinqField { Name = item.Key, Type = item.Value.PropertyType , Property = item.Value });
            }
        }

        public LinqFields(Dictionary<string, Type> fields)
        {
            foreach (var item in fields.OrderBy(x => x.Key))
            {
                this.Add(new LinqField { Name = item.Key, Type = item.Value });
            }
        }

    }

}