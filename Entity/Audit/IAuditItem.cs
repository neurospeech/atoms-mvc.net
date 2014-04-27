using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity.Audit
{
    public interface IAuditItem
    {
        string Fields { get; set; }
        string Action { get; set; }

        System.DateTime AuditTime { get; set; }

        string IPAddress { get; set; }

        string Username { get; set; }

        string TableName { get; set; }

        string TableKey { get; set; }

        string Links { get; set; }
    }
}
