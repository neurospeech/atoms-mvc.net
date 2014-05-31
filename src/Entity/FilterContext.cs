using NeuroSpeech.WebAtoms.Entity.Audit;
using NeuroSpeech.WebAtoms.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NeuroSpeech.WebAtoms.Entity
{
    public class FilterContext
    {
        public ISecureRepository DB { get; set; }
        public BaseSecurityContext Context { get; set; }
    }
}
