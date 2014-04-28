using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity.Audit
{
    public interface IAuditContext : IDisposable
    {
        string GetIPAddress();

        string GetUsername();

        IAuditItem CreateNew();

        void AddAudit(IAuditItem item);

        void SaveChanges();
    }
}
