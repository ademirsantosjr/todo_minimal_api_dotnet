using TodoMinimalApi.DTOs;

namespace TodoMinimalApi.Services.Auth
{
    public interface IAuthService
    {
        Task<UserViewDto> RegisterUserAsync(UserRegistrationDto userRegistrationDto);
    }
}
