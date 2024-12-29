namespace TodoMinimalApi.Exceptions
{
    public class TodoNotFoundException(string message) : Exception(message)
    {
    }
}
