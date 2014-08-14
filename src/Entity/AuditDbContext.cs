using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc.Entity
{
    public abstract class AuditDbContext : DbContext, IAuditContext
    {

        public AuditDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {

        }

        public class Audit : IAuditItem
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public long AuditID { get; set; }

            [StringLength(200, ErrorMessage = @"Username cannot exceed 200 characters")]
            public string Username { get; set; }
            [StringLength(50, ErrorMessage = @"IPAddress cannot exceed 50 characters")]
            public string IPAddress { get; set; }
            [StringLength(50, ErrorMessage = @"TableName cannot exceed 50 characters")]
            public string TableName { get; set; }
            [StringLength(400, ErrorMessage = @"TableKey cannot exceed 400 characters")]
            public string TableKey { get; set; }
            public DateTime AuditTime { get; set; }
            public string Fields { get; set; }
            public string Links { get; set; }
            [StringLength(20, ErrorMessage = @"Action cannot exceed 20 characters")]
            public string Action { get; set; }
        }

        public DbSet<Audit> Audits { get; set; }

        public string UserName { get; set; }

        public string IPAddress { get; set; }


        IAuditItem IAuditContext.CreateNew()
        {
            var a = new Audit();
            Audits.Add(a);
            a.IPAddress = IPAddress;
            a.Username = UserName;
            return a;
        }


        void IAuditContext.SaveChanges()
        {
            this.SaveChanges();
        }

        System.Threading.Tasks.Task<int> IAuditContext.SaveChangesAsync()
        {
            return this.SaveChangesAsync();
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
        }
    }
}
