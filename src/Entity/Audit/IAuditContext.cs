using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NeuroSpeech.Atoms.Entity.Audit
{
    public interface IAuditContext : IDisposable
    {

        IAuditItem CreateNew();

        void SaveChanges();

        Task<int> SaveChangesAsync();
    }
}
