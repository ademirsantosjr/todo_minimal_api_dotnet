namespace TodoMinimalApi.Exceptions
{
    public class UserNotFoundException(string message) : Exception(message)
    {
    }
}
