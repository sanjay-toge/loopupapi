public class UpdateUser
{
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Image { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
