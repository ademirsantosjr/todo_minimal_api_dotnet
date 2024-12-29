namespace TodoMinimalApi.Services.User
{
    public interface IUserService
    {
        public Task ApprovePenddingUserById(int userId);
    }
}
