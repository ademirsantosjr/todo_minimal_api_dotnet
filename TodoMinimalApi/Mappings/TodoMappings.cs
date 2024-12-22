using System.Runtime.CompilerServices;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Mappings
{
    public static class TodoMappings
    {
        public static TodoDto ToDto(this Todo todo)
        {
            return new TodoDto
            {
                Title = todo.Title,
                Description = todo.Description,
                CompletedAt = todo.CompletedAt
            };
        }

        public static TodoViewDto ToViewDto(this Todo todo)
        {
            return new TodoViewDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                CreatedAt = todo.CreatedAt,
                CompletedAt = todo.CompletedAt,
                UserId = todo.UserId
            };
        }

        public static Todo ToModel(this TodoDto todoDto, int userId)
        {
            return new Todo
            {
                Title = todoDto.Title,
                Description = todoDto.Description,
                CompletedAt = todoDto.CompletedAt,
                UserId = userId
            };
        }
    }
}
