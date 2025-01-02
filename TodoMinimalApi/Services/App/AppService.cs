using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Exceptions;
using TodoMinimalApi.Mappings;

namespace TodoMinimalApi.Services.App
{
    public class AppService : IAppService
    {
        private readonly TodoDbContext _dbContext;

        public AppService(TodoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SetupAdmin(UserRegistrationDto userRegistrationDto)
        {
            if (_dbContext.Users.Any())
            {
                throw new AppAlreadyConfiguredException("A aplicação já foi configurada.");
            }

            var roleAdmin = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "ADMIN");
            if (roleAdmin == null)
            {
                throw new RoleNotFoundException("Role ADMIN não encontrada");
            }

            var passwordHasher = new PasswordHasher<Models.User>();
            var admin = userRegistrationDto.ToModel();
            admin.Role = roleAdmin;

            _dbContext.Users.Add(admin);
            await _dbContext.SaveChangesAsync();
        }
    }
}
