using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Data
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

        public DbSet<Todo> Todos => Set<Todo>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Users)
                .WithOne(u => u.Role)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Todos)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "ADMIN" },
                new Role { Id = 2, Name = "PENDING"},
                new Role { Id = 3, Name = "USER" }
            );
        }
    }
}
