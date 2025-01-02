using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;

namespace TodoMinimalApi.Services.Auth
{
    public interface IAuthService
    {
        Task<UserViewDto> RegisterLoginAsync(UserRegistrationDto userRegistrationDto);
        Task<IResult> LoginAsync(UserLogin userLogin);
    }
}
