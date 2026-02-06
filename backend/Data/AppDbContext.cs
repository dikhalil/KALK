using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Player> Players { get; set; }  
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Question> Questions { get; set; }
    }
}
