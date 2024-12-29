namespace TodoMinimalApi.Exceptions
{
    public class UserAlreadyExistsException(string message) : Exception(message)
    {
    }
}
