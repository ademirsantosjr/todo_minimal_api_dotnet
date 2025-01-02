namespace TodoMinimalApi.Exceptions
{
    public class CompletedAtNotValidException(string message) : Exception(message)
    {
    }
}
