using NeuroSpeech.Atoms.Mvc.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace AppFramework.Areas.Admin.Models
{
    public class AdminDbContext : AtomDbContext
    {

        public AdminDbContext(): base("AdminDbContext")
        {

        }

        public DbSet<ApiRole> ApiRoles { get; set; }

        public DbSet<ApiRoute> ApiRoutes { get; set; }

        public DbSet<ApiUser> ApiUsers { get; set; }

    }


    [Table("Api.ApiRoles")]
    public class ApiRole
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleID { get; set; }

        [StringLength(50)]
        public string RoleName { get; set; }

        public string SecurityContext { get; set; }
    }

    [Table("Api.ApiRoutes")]
    public class ApiRoute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RouteID { get; set; }

        [StringLength(100)]
        public string RouteName { get; set; }
    }


    [Table("Api.ApiUsers")]
    public class ApiUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        public string EmailAddress { get; set; }

        public string PasswordSHA1 { get; set; }
    }
}