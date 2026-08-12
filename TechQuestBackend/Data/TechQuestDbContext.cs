using Microsoft.EntityFrameworkCore;
using TechQuestBackend.Models;

namespace TechQuestBackend.Data
{
    public class TechQuestDbContext : DbContext
    {
        public TechQuestDbContext(
            DbContextOptions<TechQuestDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<Professor> Professors { get; set; }

        public DbSet<Admin> Admins { get; set; }

        public DbSet<OTP> OTPs { get; set; }

        public DbSet<PendingStudent> PendingStudents { get; set; }
    }
}