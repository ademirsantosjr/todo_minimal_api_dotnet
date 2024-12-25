namespace TodoMinimalApi.DTOs
{
    public class TodoUpdateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; } = null;
    }
}
