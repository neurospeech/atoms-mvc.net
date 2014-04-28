using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Transactions;
using NeuroSpeech.Atoms.Entity;
using System.IO;

namespace NeuroSpeech.Atoms.Mvc
{
    [Serializable]
    public class CacheItem
    {
        public DateTime LastModified { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
    }
}
