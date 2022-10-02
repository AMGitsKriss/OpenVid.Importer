using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Database.Models
{
    public partial class OpenVidContext : DbContext
    {
        public OpenVidContext()
        {
        }

        public OpenVidContext(DbContextOptions<OpenVidContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Video> Video { get; set; }
        public virtual DbSet<VideoEncodeQueue> VideoEncodeQueue { get; set; }
        public virtual DbSet<VideoSegmentQueue> VideoSegmentQueue { get; set; }
        public virtual DbSet<VideoSegmentQueueItem> VideoSegmentQueueItem { get; set; }
        public virtual DbSet<VideoSource> VideoSource { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("name=ConnectionStrings:DefaultDatabase");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Description)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.Length).HasColumnType("time(0)");

                entity.Property(e => e.MetaText).IsUnicode(false);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256)
                    .IsUnicode(false);

                entity.Property(e => e.RatingId).HasColumnName("RatingID");
            });

            modelBuilder.Entity<VideoEncodeQueue>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Encoder)
                    .IsRequired()
                    .HasMaxLength(16)
                    .IsUnicode(false);

                entity.Property(e => e.InputDirectory)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.OutputDirectory)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.PlaybackFormat)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.RenderSpeed)
                    .IsRequired()
                    .HasMaxLength(16)
                    .IsUnicode(false);

                entity.Property(e => e.VideoFormat)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.Property(e => e.VideoId).HasColumnName("VideoID");

                entity.HasOne(d => d.Video)
                    .WithMany(p => p.VideoEncodeQueue)
                    .HasForeignKey(d => d.VideoId)
                    .HasConstraintName("FK_VideoEncodeQueue_Video");
            });

            modelBuilder.Entity<VideoSegmentQueue>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.VideoId).HasColumnName("VideoID");

                entity.HasOne(d => d.Video)
                    .WithMany(p => p.VideoSegmentQueue)
                    .HasForeignKey(d => d.VideoId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VideoSegmentQueue_Video");
            });

            modelBuilder.Entity<VideoSegmentQueueItem>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.VideoId).HasColumnName("VideoID");

                entity.Property(e => e.ArgInputFolder)
                    .IsRequired()
                    .IsUnicode(true);

                entity.Property(e => e.ArgStreamFolder)
                    .IsRequired()
                    .IsUnicode(true);

                entity.Property(e => e.ArgStream)
                    .IsRequired()
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.ArgStreamId)
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.VideoSegmentQueueId).HasColumnName("VideoSegmentQueueId");

                entity.HasOne(d => d.Video)
                    .WithMany(p => p.VideoSegmentQueueItem)
                    .HasForeignKey(d => d.VideoId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VideoSegmentQueueItem_Video");

                entity.HasOne(d => d.VideoSegmentQueue)
                    .WithMany(p => p.VideoSegmentQueueItem)
                    .HasForeignKey(d => d.VideoSegmentQueueId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_VideoSegmentQueueItem_VideoSegmentQueue");
            });

            modelBuilder.Entity<VideoSource>(entity =>
            {
                entity.HasIndex(e => e.Md5)
                    .HasName("IX_VideoSource_Unique")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Extension)
                    .IsRequired()
                    .HasMaxLength(4)
                    .IsUnicode(false);

                entity.Property(e => e.Md5)
                    .IsRequired()
                    .HasColumnName("MD5")
                    .HasMaxLength(32)
                    .IsUnicode(false);

                entity.Property(e => e.VideoId).HasColumnName("VideoID");

                entity.HasOne(d => d.Video)
                    .WithMany(p => p.VideoSource)
                    .HasForeignKey(d => d.VideoId)
                    .HasConstraintName("FK_VideoSource_Video");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
