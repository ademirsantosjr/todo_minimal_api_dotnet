using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Mappings;

namespace TodoMinimalApi.Services
{
    public class TodoService : ITodoService
    {
        private readonly TodoDbContext _dbContext;

        public TodoService(TodoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<TodoViewDto>> GetUserTodosAsync(int userId)
        {
            var todos = await _dbContext.Todos.Where(t => t.UserId == userId).ToListAsync();
            return todos.Select(t => t.ToViewDto());
        }

        public async Task<TodoViewDto?> GetTodoByIdAsync(int userId, int todoId)
        {
            var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
            return todo?.ToViewDto();
        }

        public async Task<TodoViewDto> CreateTodoAsync(int userId, TodoDto todoDto)
        {
            var todo = todoDto.ToModel(userId);

            _dbContext.Todos.Add(todo);
            await _dbContext.SaveChangesAsync();

            return todo.ToViewDto();
        }

        public async Task<bool> UpdateTodoAsync(int userId, int todoId, TodoDto updatedTodoDto)
        {
            var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
            if (todo == null) return false;

            todo.Title = updatedTodoDto.Title;
            todo.Description = updatedTodoDto.Description;
            todo.CompletedAt = updatedTodoDto.CompletedAt;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTodoAsync(int userId, int todoId)
        {
            var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
            if (todo == null) return false;

            _dbContext.Todos.Remove(todo);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
