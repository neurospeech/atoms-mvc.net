using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomsTest
{
    class Program
    {
        static void Main(string[] args)
        {

            Database.SetInitializer<FluentModel>(new DropCreateDatabaseAlways<FluentModel>());

            using (FluentModel db = new FluentModel()) {

                
                Console.WriteLine(db.Products.Count());

                
            }

            using (MModel db = new MModel()) {
                Console.WriteLine(db.Products.Count());
                Console.ReadLine();
            }


        }



    }


    public class MModel : DbContext {

        public MModel():base("FluentModel")
        {

        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        public class Product {
            [Key]
            public long ProductID { get; set; }

            [StringLength(200)]
            public string Name { get; set; }

            public string Temp { get; set; }


            [InverseProperty("Product")]
            public List<Order> Orders { get; set; }
        }

        public class Order {
            [Key]
            public string OrderID { get; set; }

            public long? ProductID { get; set; }

            [ForeignKey("ProductID")]
            [InverseProperty("Orders")]
            public Product Product { get; set; }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Console.WriteLine("Model Created");
        }
    }


    public class FluentModel : DbContext {

        public DbSet<FluentProduct> Products { get; set; }

        public DbSet<FluentOrder> Orders { get; set; }



        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new FluentProduct.Configuration());
            modelBuilder.Configurations.Add(new FluentOrder.Configuration());
        }

    }

    public class FluentProduct {

        public long ProductID { get; set; }

        public string Name { get; set; }

        public string Temp { get; set; }

        [NotMapped]
        public string TemporaryKey { get; set; }

        public List<FluentOrder> Orders { get; set; }


        public class Configuration : EntityTypeConfiguration<FluentProduct> {
            public Configuration()
            {
                this.ToTable("Products");
                this.HasKey(x => x.ProductID);

                this.Property(x => x.Name)
                        .HasMaxLength(200);

                //this.HasMany(x => x.Orders)
                    //.WithOptional(c => c.Product);
            }
        }
    }

    public class FluentOrder {

        public long OrderID { get; set; }

        public long? ProductID { get; set; }


        public FluentProduct Product { get; set; }

        public class Configuration : EntityTypeConfiguration<FluentOrder> {
            public Configuration()
            {
                this.ToTable("Orders");
                this.HasKey(x => x.OrderID);

                this.HasOptional(x => x.Product)
                    .WithMany(x => x.Orders)
                    .HasForeignKey(x => x.ProductID);
            }
        }
    }
}
