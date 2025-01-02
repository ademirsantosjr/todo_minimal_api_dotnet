namespace TodoMinimalApi.Models

{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public ICollection<Todo> Todos { get; set; } = new List<Todo>();
    }
}
