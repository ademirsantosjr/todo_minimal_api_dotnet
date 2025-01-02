using TodoMinimalApi.DTOs;

namespace TodoMinimalApi.Services.App
{
    public interface IAppService
    {
        public Task SetupAdmin(UserRegistrationDto userRegistrationDto);
    }
}
