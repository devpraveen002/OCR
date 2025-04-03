using Microsoft.EntityFrameworkCore;
using PDFOCRProcessor.Infrastructure.Data.Entities;

namespace PDFOCRProcessor.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<DocumentEntity> Documents { get; set; }
        public DbSet<DocumentFieldEntity> DocumentFields { get; set; }
        public DbSet<UserEntity> Users { get; set; }

       protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentFieldEntity>()
                .HasOne(df => df.Document)
                .WithMany(d => d.Fields)
                .HasForeignKey(df => df.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<DocumentEntity>()
                .HasIndex(d => d.FileName);

            modelBuilder.Entity<DocumentEntity>()
                .HasIndex(d => d.DocumentType);

            modelBuilder.Entity<DocumentFieldEntity>()
                .HasIndex(df => new { df.DocumentId, df.Name });

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.Email)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}