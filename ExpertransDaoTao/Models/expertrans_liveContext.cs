using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ExpertransDaoTao.Models
{
    public partial class expertrans_liveContext : DbContext
    {
        public expertrans_liveContext()
        {
        }

        public expertrans_liveContext(DbContextOptions<expertrans_liveContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Students> Students { get; set; }
        public virtual DbSet<Teachers> Teachers { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        //        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //        {
        //            if (!optionsBuilder.IsConfigured)
        //            {
        //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
        //                optionsBuilder.UseSqlServer("Data Source=103.124.93.173;Initial Catalog=expertrans_live;Persist Security Info=True;User ID=sa;Password=DEI2029hnA");
        //            }
        //        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Students>(entity =>
            {
                entity.Property(e => e.BeginDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy).HasMaxLength(500);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateOfBirth).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.IdentityImg).HasColumnName("IdentityIMG");

                entity.Property(e => e.IdentityImg2).HasColumnName("IdentityIMG2");

                entity.Property(e => e.Mobile).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(500);

                entity.Property(e => e.SGuid).HasColumnName("sGuid");

                entity.Property(e => e.UserName).HasMaxLength(500);
            });



            modelBuilder.Entity<Teachers>(entity =>
            {
                entity.Property(e => e.BeginDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy).HasMaxLength(500);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateOfBirth).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.IdentityImg).HasColumnName("IdentityIMG");

                entity.Property(e => e.IdentityImg2).HasColumnName("IdentityIMG2");

                entity.Property(e => e.Mobile).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(500);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.UserName).HasMaxLength(500);
            });



            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.BeginDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedBy).HasMaxLength(500);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DateOfBirth).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.IdentityImg).HasColumnName("IdentityIMG");

                entity.Property(e => e.IdentityImg2).HasColumnName("IdentityIMG2");

                entity.Property(e => e.Mobile).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(500);

                entity.Property(e => e.UserName).HasMaxLength(500);
            });



            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
