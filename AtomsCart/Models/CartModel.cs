using NeuroSpeech.Atoms.Mvc.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace AtomsCart.Models
{
    public class CartModel : AtomDbContext
    {

        static CartModel() {
            Database.SetInitializer(new CartModelInitializer());
        }


        /// <summary>
        /// Access id of current account
        /// </summary>
        public long UserID { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

    }


    [Table("Accounts")]
    public class Account {


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AccountID { get; set; }

        [StringLength(50)]
        public string AccountName { get; set; }

        [StringLength(10)]
        public string AccountType { get; set; }

        [Required]
        [StringLength(200)]
        public string Username { get; set; }

        [StringLength(100)]
        public string Password { get; set; }

        [InverseProperty("Customer")]
        [ScriptIgnore]
        public List<Order> Orders { get; set; } = new List<Order>();




    }

    [Table("Products")]
    public class Product {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ProductID { get; set; }

        [StringLength(200)]
        public string ProductName { get; set; }


        [InverseProperty("Product")]
        [ScriptIgnore]
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    [Table("Orders")]
    public class Order {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrderID { get; set; }

        public string Description { get; set; }

        public long? CustomerID { get; set; }

        public decimal Total { get; set; }

        [InverseProperty("Order")]
        [ScriptIgnore]
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        [ForeignKey("CustomerID")]
        [InverseProperty("Orders")]
        public Account Customer { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateUpdated { get; set; }

    }

    [Table("OrderItems")]
    public class OrderItem {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OrderItemID { get; set; }

        public long OrderID { get; set; }

        [ForeignKey("OrderID")]
        [InverseProperty("OrderItems")]
        public Order Order { get; set; }

        public long ProductID { get; set; }

        [ForeignKey("ProductID")]
        [InverseProperty("OrderItems")]
        public Product Product { get; set; }

        public decimal Amount { get; set; }

        public decimal Quantity { get; set; }

        public decimal Total { get; set; }
    }
}