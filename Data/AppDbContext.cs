using Microsoft.EntityFrameworkCore;
using QikHubAPI.Models;

namespace QikHubAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<ServicePro> ServicePros { get; set; }
        public DbSet<DeliveryPerson> DeliveryPersons { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ServiceBooking> ServiceBookings { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Advertise> Advertises { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.Code)
                .IsUnique();

            // Fix cascade paths - Use NoAction instead of Cascade
            modelBuilder.Entity<Seller>()
                .HasOne(s => s.User)
                .WithOne(u => u.Seller)
                .HasForeignKey<Seller>(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ServicePro>()
                .HasOne(sp => sp.User)
                .WithMany()
                .HasForeignKey(sp => sp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeliveryPerson>()
                .HasOne(d => d.User)
                .WithOne(u => u.DeliveryPerson)
                .HasForeignKey<DeliveryPerson>(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Provider)
                .WithMany()
                .HasForeignKey(s => s.ProviderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Seller)
                .WithMany()
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Customer)
                .WithMany()
                .HasForeignKey(sb => sb.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Provider)
                .WithMany()
                .HasForeignKey(sb => sb.ProviderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Service)
                .WithMany()
                .HasForeignKey(r => r.ServiceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Wishlist>()
                .HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}