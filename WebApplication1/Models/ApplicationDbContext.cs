using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ClientDetails> ClientDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Trainer>().ToTable("Trainers");
            modelBuilder.Entity<Inventory>().ToTable("Inventory");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");
            modelBuilder.Entity<ClientDetails>().ToTable("ClientDetails");

            modelBuilder.Entity<Trainer>()
                .Property(t => t.ProductionCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Trainer>()
                .Property(t => t.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ClientDetails)
                .WithMany()
                .HasForeignKey(o => o.ClientDetailsId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Trainer>()
                .HasOne(t => t.Inventory)
                .WithOne(i => i.Trainer)
                .HasForeignKey<Inventory>(i => i.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);


            base.OnModelCreating(modelBuilder);
        }
    }
}
