using Microsoft.EntityFrameworkCore;
using OurSpace.API.Models.Entities;

namespace OurSpace.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Couple> Couples => Set<Couple>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<AudioMessage> AudioMessages => Set<AudioMessage>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TimeCapsule> TimeCapsules => Set<TimeCapsule>();
    public DbSet<OutboxEmail> OutboxEmails => Set<OutboxEmail>();
    public DbSet<JobLease> JobLeases => Set<JobLease>();
    public DbSet<AttemptCounter> AttemptCounters => Set<AttemptCounter>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(30);
            entity.Property(u => u.Email).HasMaxLength(254);
            entity.Property(u => u.PendingEmail).HasMaxLength(254);

            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PairingCode).IsUnique().HasFilter("[PairingCode] IS NOT NULL");
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
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrustedDevice>(entity =>
        {
            entity.HasIndex(d => d.Token).IsUnique();

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.HasOne(e => e.Couple)
                .WithMany()
                .HasForeignKey(e => e.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.CoupleId, e.EventDate });
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.Property(p => p.Caption).HasMaxLength(300);

            entity.HasOne(p => p.Couple)
                .WithMany()
                .HasForeignKey(p => p.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.UploadedByUser)
                .WithMany()
                .HasForeignKey(p => p.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.CoupleId, p.TakenAt });
        });

        modelBuilder.Entity<AudioMessage>(entity =>
        {
            entity.Property(a => a.Caption).HasMaxLength(300);

            entity.HasOne(a => a.Couple)
                .WithMany()
                .HasForeignKey(a => a.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.UploadedByUser)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.CoupleId, a.RecordedAt });
        });

        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.Property(w => w.Title).HasMaxLength(200);

            entity.HasOne(w => w.Couple)
                .WithMany()
                .HasForeignKey(w => w.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.CreatedByUser)
                .WithMany()
                .HasForeignKey(w => w.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(w => new { w.CoupleId, w.IsFulfilled, w.CreatedAt });
        });

        modelBuilder.Entity<TimeCapsule>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200);
            entity.Property(c => c.Message).HasMaxLength(5000);

            entity.HasOne(c => c.Couple)
                .WithMany()
                .HasForeignKey(c => c.CoupleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.CoupleId, c.OpenAt });
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.EntityType).HasMaxLength(20);
            entity.Property(n => n.EntityTitle).HasMaxLength(200);

            entity.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.ActorUser)
                .WithMany()
                .HasForeignKey(n => n.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new { n.RecipientUserId, n.ReadAt, n.CreatedAt });
        });

        modelBuilder.Entity<OutboxEmail>(entity =>
        {
            entity.Property(o => o.ToEmail).HasMaxLength(254);
            entity.Property(o => o.Subject).HasMaxLength(300);
            entity.Property(o => o.LastError).HasMaxLength(1000);

            entity.HasIndex(o => new { o.SentAt, o.NextAttemptAt });
        });

        modelBuilder.Entity<JobLease>(entity =>
        {
            entity.HasKey(l => l.Name);
            entity.Property(l => l.Name).HasMaxLength(100);
            entity.Property(l => l.Owner).HasMaxLength(100);
        });

        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.Property(j => j.Type).HasMaxLength(50);
            entity.Property(j => j.LastError).HasMaxLength(1000);

            entity.HasIndex(j => new { j.Status, j.Id });
        });

        modelBuilder.Entity<AttemptCounter>(entity =>
        {
            entity.Property(c => c.Bucket).HasMaxLength(200);
            entity.HasIndex(c => new { c.Bucket, c.WindowStart }).IsUnique();
        });
    }
}
