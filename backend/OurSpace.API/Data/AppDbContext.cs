using Microsoft.EntityFrameworkCore;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Couple> Couples => Set<Couple>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<AudioMessage> AudioMessages => Set<AudioMessage>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PairingCode).IsUnique().HasFilter("[PairingCode] IS NOT NULL");
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<Couple>(entity =>
        {
            entity.HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
            entity.HasQueryFilter(c => !c.User1.IsDeleted && !c.User2.IsDeleted);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(r => !r.User.IsDeleted);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Couple)
                .WithMany()
                .HasForeignKey(e => e.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.CoupleId, e.EventDate });
            entity.HasQueryFilter(e => !e.Couple.User1.IsDeleted && !e.Couple.User2.IsDeleted);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasOne(p => p.Couple)
                .WithMany()
                .HasForeignKey(p => p.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.UploadedByUser)
                .WithMany()
                .HasForeignKey(p => p.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.CoupleId, p.TakenAt });
            entity.HasQueryFilter(p => !p.Couple.User1.IsDeleted && !p.Couple.User2.IsDeleted);
        });

        modelBuilder.Entity<AudioMessage>(entity =>
        {
            entity.HasOne(a => a.Couple)
                .WithMany()
                .HasForeignKey(a => a.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.UploadedByUser)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.CoupleId, a.RecordedAt });
            entity.HasQueryFilter(a => !a.Couple.User1.IsDeleted && !a.Couple.User2.IsDeleted);
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasOne(w => w.Couple)
                .WithMany()
                .HasForeignKey(w => w.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.CreatedByUser)
                .WithMany()
                .HasForeignKey(w => w.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(w => new { w.CoupleId, w.IsFulfilled, w.CreatedAt });
            entity.HasQueryFilter(w => !w.Couple.User1.IsDeleted && !w.Couple.User2.IsDeleted);
        });
    }
}
