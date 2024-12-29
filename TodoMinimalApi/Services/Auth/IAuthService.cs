using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Services.Auth
{
    public interface IAuthService
    {
        Task<UserViewDto> RegisterUserAsync(UserRegistrationDto userRegistrationDto);
        Task<IResult> LoginAsync(UserLogin userLogin);
    }
}
