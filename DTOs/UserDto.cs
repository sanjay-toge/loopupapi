namespace LoopUpAPI.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public float Rating { get; set; } = 0;
        public string Bio { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}