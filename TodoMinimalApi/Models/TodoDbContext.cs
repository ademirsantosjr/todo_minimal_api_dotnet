using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Models
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

        public DbSet<Todo> Todos => Set<Todo>();
        public DbSet<User> Users => Set<User>();
    }
}
