using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Exceptions;
using TodoMinimalApi.Mappings;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly TodoDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(IConfiguration configuration, TodoDbContext dbContext, IPasswordHasher<User> passwordHasher)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
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

        public async Task<IResult> LoginAsync(UserLogin userLogin)
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == userLogin.Email);

            if (user == null) return Results.Unauthorized();

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, userLogin.Password);

            if (result == PasswordVerificationResult.Success)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.Name)
                };

                var secretKey = _configuration["JwtSettings:Key"];
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "TodoMinimalApi",
                    audience: "TodoMinimalApi",
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                return TypedResults.Ok(new { Token = tokenString });
            }

            return TypedResults.Unauthorized();
        }
    }
}
