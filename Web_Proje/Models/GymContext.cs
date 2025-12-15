using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web_Proje.Models
{
    public class GymContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public GymContext(DbContextOptions<GymContext> options)
            : base(options)
        { }

        public DbSet<Gym> Gyms { get; set; }

        public DbSet<Trainer> Trainers { get; set; }

        public DbSet<Services> Services { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<TrainerService> TrainerService { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {

            builder.Entity<TrainerService>()
        .HasKey(ts => new { ts.TrainerId, ts.ServiceId }); // composite key

            builder.Entity<TrainerService>()
                .HasOne(ts => ts.trainer)
                .WithMany(t => t.TrainerServices)
                .HasForeignKey(ts => ts.TrainerId);

            builder.Entity<TrainerService>()
                .HasOne(ts => ts.service)
                .WithMany(s => s.TrainerServices)
                .HasForeignKey(ts => ts.ServiceId);
            base.OnModelCreating(builder);
            builder.Entity<Trainer>()
                .HasOne(t => t.Gym)
                .WithMany(g => g.Trainers)
                .HasForeignKey(t => t.GymId)
                .OnDelete(DeleteBehavior.Cascade); //Cascade silme hatası düzeltildi
        }
    }
}
