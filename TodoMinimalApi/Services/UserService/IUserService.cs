namespace TodoMinimalApi.Services.UserService
{
    public interface IUserService
    {
        public Task ApprovePenddingUserById(int userId);
    }
}
