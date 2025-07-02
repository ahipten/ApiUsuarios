using Microsoft.EntityFrameworkCore;
using Models;

namespace Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Cultivo> Cultivos => Set<Cultivo>();
        public DbSet<Sensor> Sensores => Set<Sensor>();
        public DbSet<Lectura> Lecturas => Set<Lectura>();
    }
}
