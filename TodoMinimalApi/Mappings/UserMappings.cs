using Microsoft.AspNetCore.Identity;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Mappings
{
    public static class UserMappings
    {
        public static UserViewDto ToUserViewDto(this User user)
        {
            return new UserViewDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
            };
        }

        public static User ToModel(this UserRegistrationDto userRegistrationDto)
        {
            var passwordHasher = new PasswordHasher<User>();

            return new User
            {
                Name = userRegistrationDto.Name,
                Email = userRegistrationDto.Email,
                PasswordHash = passwordHasher.HashPassword(null, userRegistrationDto.Password)
            };
        }
    }
}
