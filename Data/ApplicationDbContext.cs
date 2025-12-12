using GymProject1.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace GymProject1.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Veritabanı Tablolarımız
        public DbSet<Salon> Salons { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<TrainerService> TrainerServices { get; set; } // Ara Tablo
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Salon - Trainer ilişkisi (Cascade path hatasını önlemek için Restrict)
            builder.Entity<Trainer>()
                .HasOne(t => t.Salon)
                .WithMany(s => s.Trainers)
                .HasForeignKey(t => t.SalonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Salon - Service ilişkisi (Cascade path hatasını önlemek için Restrict)
            builder.Entity<Service>()
                .HasOne(s => s.Salon)
                .WithMany(salon => salon.Services)
                .HasForeignKey(s => s.SalonId)
                .OnDelete(DeleteBehavior.Restrict);

            // TrainerService için Çoka-Çok İlişki (Composite Key) Tanımlaması
            builder.Entity<TrainerService>()
                .HasKey(ts => new { ts.TrainerId, ts.ServiceId });

            builder.Entity<TrainerService>()
                .HasOne(ts => ts.Trainer)
                .WithMany(t => t.TrainerServices)
                .HasForeignKey(ts => ts.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TrainerService>()
                .HasOne(ts => ts.Service)
                .WithMany(s => s.TrainerServices)
                .HasForeignKey(ts => ts.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Trainer ilişkisi (Cascade path hatasını önlemek için Restrict)
            builder.Entity<Appointment>()
                .HasOne(a => a.Trainer)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment - Service ilişkisi (Cascade path hatasını önlemek için Restrict)
            builder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service.Price için decimal precision belirtme (Para için: 18 toplam basamak, 2 ondalık)
            builder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);
        }
    }
}
