using Microsoft.EntityFrameworkCore;
using TodoMinimalApi.Data;
using TodoMinimalApi.Exceptions;

namespace TodoMinimalApi.Services.User
{
    public class UserService : IUserService
    {
        private readonly TodoDbContext _dbContext;

        public UserService(TodoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ApprovePenddingUserById(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new UserNotFoundException("Usuário ID " + userId + " não encontrado.");
            }

            string roleName = "USER";
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                throw new RoleNotFoundException("Papel de usuário " + roleName + " não encontrado.");
            }

            user.Role = role;
            await _dbContext.SaveChangesAsync();
        }
    }
}
