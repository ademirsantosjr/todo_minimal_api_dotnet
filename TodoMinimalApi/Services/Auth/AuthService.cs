using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Exceptions;
using TodoMinimalApi.Mappings;

namespace TodoMinimalApi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly TodoDbContext _dbContext;

        public AuthService(TodoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserViewDto> RegisterUserAsync(UserRegistrationDto userRegistrationDto)
        {
            var foundUser = await _dbContext.Users.FirstOrDefaultAsync(u =>  u.Email == userRegistrationDto.Email);
            if (foundUser != null)
            {
                throw new UserAlreadyExistsException("Já existe um usuário com este email.");
            }

            var standardRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "PENDING")
                ?? throw new RoleNotFoundException("O papel padrão de usuário não foi encontrado.");

            var newUser = userRegistrationDto.ToModel();
            newUser.Role = standardRole;

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            return newUser.ToUserViewDto();
        }
    }
}
