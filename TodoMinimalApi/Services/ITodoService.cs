using TodoMinimalApi.DTOs;

namespace TodoMinimalApi.Services
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoViewDto>> GetUserTodosAsync(int userId);
        Task<TodoViewDto?> GetTodoByIdAsync(int userId, int todoId);
        Task<TodoViewDto> CreateTodoAsync(int userId, TodoDto todoDto);
        Task UpdateTodoAsync(int userId, int todoId, TodoUpdateDto updatedTodoDto);
        Task<bool> DeleteTodoAsync(int userId, int todoId);
    }
}
