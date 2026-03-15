using Microsoft.EntityFrameworkCore;

namespace CashDesk.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Đại diện cho bảng EventStreams trong SQL
    public DbSet<EventEntity> EventStreams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Khóa chính
        modelBuilder.Entity<EventEntity>().HasKey(e => e.Id);
        // Bắt buộc phải có data JSON, không được để trống
        modelBuilder.Entity<EventEntity>().Property(e => e.EventData).IsRequired();
    }
}