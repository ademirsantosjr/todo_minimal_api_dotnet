namespace TodoMinimalApi.Exceptions
{
    public class CompletedAtNotValidException : Exception
    {
        public CompletedAtNotValidException(string message) : base(message)
        {
        }
    }
}
