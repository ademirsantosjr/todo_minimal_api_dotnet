﻿namespace TodoMinimalApi.DTOs
{
    public class TodoDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty ;
        public DateTime? CompletedAt { get; set; }
        public int UserId { get; set; }
    }
}