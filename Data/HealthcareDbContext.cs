
using Healthcare.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Healthcare.API.Data
{
    public class HealthcareDbContext : IdentityDbContext<ApplicationUser>
    {
        public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Provider> Providers { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<MedicalRecord> MedicalRecords { get; set; }

        public DbSet<MedicalRecordAuditLog> MedicalRecordAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Appointment - Patient relationship
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Provider relationship
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Provider)
                .WithMany()
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Medical Record - Patient relationship
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Medical Record - Audit Log relationship
            modelBuilder.Entity<MedicalRecordAuditLog>()
                .HasOne(a => a.MedicalRecord)
                .WithMany()
                .HasForeignKey(a => a.MedicalRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            // Provider - Identity relationship
            modelBuilder.Entity<Provider>()
                .HasOne(p => p.ApplicationUser)
                .WithOne()
                .HasForeignKey<Provider>(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient - Identity relationship
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.ApplicationUser)
                .WithOne()
                .HasForeignKey<Patient>(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
