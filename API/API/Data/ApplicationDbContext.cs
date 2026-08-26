using API.Comments;
using API.Projects;
using API.Tickets;
using API.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.Author)
                .WithMany(u => u.AuthoredTickets)
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.ProjectId);
            entity.HasIndex(t => t.Status);
        });
        
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Commenter)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.CommenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.TicketId);
        });
        
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });
        
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Participants)
            .WithMany(u => u.Projects)
            .UsingEntity(j => j.ToTable("ProjectParticipants"));
        
        modelBuilder.Entity<Ticket>()
            .HasMany(t => t.Tags)
            .WithMany(tag => tag.Tickets)
            .UsingEntity(j => j.ToTable("TicketTags"));
        
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(utcConverter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableUtcConverter);
            }
        }
    }
}