using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Exceptions;
using TodoMinimalApi.Mappings;

namespace TodoMinimalApi.Services.Todo
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

        public async Task UpdateTodoAsync(int userId, int todoId, TodoUpdateDto updatedTodoDto)
        {
            var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (todo == null)
            {
                throw new TodoNotFoundException($"Tarefa com ID {todoId} não encontrada.");
            }

            var completedAt = updatedTodoDto.CompletedAt;

            if (completedAt.HasValue && completedAt.Value.ToUniversalTime() < todo.CreatedAt)
            {
                throw new CompletedAtNotValidException("A data de conclusão não pode ser anterior à data de criação da tarefa.");
            }

            if (completedAt.HasValue && InTheFuture(completedAt.Value))
            {
                throw new CompletedAtNotValidException("A data de conclusão não pode ser superior à data atual.");
            }

            todo.Title = updatedTodoDto.Title;
            todo.Description = updatedTodoDto.Description;
            todo.CompletedAt = completedAt.HasValue ? completedAt.Value.ToUniversalTime() : completedAt;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> DeleteTodoAsync(int userId, int todoId)
        {
            var todo = await _dbContext.Todos.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
            if (todo == null) return false;

            _dbContext.Todos.Remove(todo);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static bool InTheFuture(DateTime dateTime)
        {
            var nowWithoutSeconds = DateTime.UtcNow.AddSeconds(-dateTime.Second);
            var dateTimeWithoutSeconds = dateTime.AddSeconds(-dateTime.Second);

            return dateTimeWithoutSeconds.CompareTo(nowWithoutSeconds) > 0;
        }
    }
}
