using backend_dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<ServiceAppointment> ServiceAppointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- User ----
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.ClerkId).IsUnique();
                entity.Property(u => u.Role).HasConversion<string>();
            });

            // ---- UserSession ----
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasIndex(s => s.Token).IsUnique();
                entity.HasIndex(s => s.UserId);

                entity.HasOne(s => s.User)
                      .WithMany(u => u.Sessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---- Doctor ----
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.HasIndex(d => d.Email).IsUnique();
                entity.Property(d => d.Availability).HasConversion<string>();
            });

            // ---- Appointment ----
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasIndex(a => a.Owner);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.DoctorId);
                entity.HasIndex(a => a.SessionId);

                entity.Property(a => a.Status).HasConversion<string>();
                entity.Property(a => a.PaymentMethod).HasConversion<string>();
                entity.Property(a => a.PaymentStatus).HasConversion<string>();

                entity.HasOne(a => a.User)
                      .WithMany(u => u.Appointments)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Doctor)
                      .WithMany(d => d.Appointments)
                      .HasForeignKey(a => a.DoctorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---- ServiceAppointment ----
            modelBuilder.Entity<ServiceAppointment>(entity =>
            {
                entity.HasIndex(sa => new { sa.Date, sa.Status });
                entity.HasIndex(sa => sa.UserId);
                entity.HasIndex(sa => sa.ServiceId);
                entity.HasIndex(sa => sa.PaymentSessionId);

                entity.Property(sa => sa.Status).HasConversion<string>();
                entity.Property(sa => sa.PaymentMethod).HasConversion<string>();
                entity.Property(sa => sa.PaymentStatus).HasConversion<string>();

                entity.HasOne(sa => sa.User)
                      .WithMany(u => u.ServiceAppointments)
                      .HasForeignKey(sa => sa.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(sa => sa.Service)
                      .WithMany(s => s.ServiceAppointments)
                      .HasForeignKey(sa => sa.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}