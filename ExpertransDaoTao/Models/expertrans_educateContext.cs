using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ExpertransDaoTao.Models
{
    public partial class expertrans_educateContext : DbContext
    {
        public expertrans_educateContext()
        {
        }

        public expertrans_educateContext(DbContextOptions<expertrans_educateContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Answer> Answer { get; set; }
        public virtual DbSet<Course> Course { get; set; }
        public virtual DbSet<CourseLevel2> CourseLevel2 { get; set; }
        public virtual DbSet<CourseLevel3> CourseLevel3 { get; set; }
        public virtual DbSet<Document> Document { get; set; }
        public virtual DbSet<Homework> Homework { get; set; }
        public virtual DbSet<HomeworkHistory> HomeworkHistory { get; set; }
        public virtual DbSet<Question> Question { get; set; }
        public virtual DbSet<StudentCourse> StudentCourse { get; set; }
        public virtual DbSet<Test> Test { get; set; }
        public virtual DbSet<TestHistory> TestHistory { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.Property(e => e.Type).HasMaxLength(1000);

                entity.HasOne(d => d.Doc)
                    .WithMany(p => p.Answer)
                    .HasForeignKey(d => d.DocId)
                    .HasConstraintName("FK__Answer__DocId__3D5E1FD2");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.Answer)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__Answer__Question__3C69FB99");
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.Property(e => e.CourseDescription).HasMaxLength(200);

                entity.Property(e => e.CourseName).HasMaxLength(100);

                entity.Property(e => e.Tag).IsUnicode(false);
            });

            modelBuilder.Entity<CourseLevel2>(entity =>
            {
                entity.HasKey(e => e.CourseId2)
                    .HasName("PK__CourseLe__0AABB0FC13935B07");

                entity.Property(e => e.Description).HasMaxLength(100);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.CourseLevel2)
                    .HasForeignKey(d => d.CourseId)
                    .HasConstraintName("FK__CourseLev__Cours__22AA2996");
            });

            modelBuilder.Entity<CourseLevel3>(entity =>
            {
                entity.HasKey(e => e.CourseId3)
                    .HasName("PK__CourseLe__0AABB0FB3D899CA6");

                entity.Property(e => e.Description).HasMaxLength(100);

                entity.Property(e => e.DocumentsId).IsUnicode(false);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.Questions).IsUnicode(false);

                entity.HasOne(d => d.CourseId2Navigation)
                    .WithMany(p => p.CourseLevel3)
                    .HasForeignKey(d => d.CourseId2)
                    .HasConstraintName("FK__CourseLev__Cours__25869641");
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.DocId)
                    .HasName("PK__Document__3EF188ADEF60E3F9");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.Trace)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Type)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Homework>(entity =>
            {
                entity.Property(e => e.ExpiredDate).HasColumnType("datetime");

                entity.Property(e => e.HomeworkName).HasMaxLength(100);

                entity.Property(e => e.TotalPoint)
                    .HasMaxLength(9)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<HomeworkHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryId)
                    .HasName("PK__Homework__4D7B4ABDD451B63C");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.Property(e => e.DocIdsContent).IsUnicode(false);

                entity.Property(e => e.TrueAnswers).IsUnicode(false);

                entity.Property(e => e.Type).HasMaxLength(1000);
            });

            modelBuilder.Entity<StudentCourse>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Student_Course");
            });

            modelBuilder.Entity<Test>(entity =>
            {
                entity.Property(e => e.DocumentsId).IsUnicode(false);

                entity.Property(e => e.TotalPoint)
                    .HasMaxLength(9)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<TestHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryId)
                    .HasName("PK__TestHist__4D7B4ABDE55B623C");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
