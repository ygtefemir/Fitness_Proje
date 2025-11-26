using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web_Proje.Models
{
    public class GymContext : IdentityDbContext<User, IdentityRole<int>, int>
    {

        public DbSet<Gym> Gyms { get; set; }

        public DbSet<Trainer> Trainers { get; set; }

        public DbSet<Services> Services { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder opt)
        {
            opt.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GymSystemDB;Trusted_Connection=True;MultipleActiveResultSets=true");

        }
    }
}
