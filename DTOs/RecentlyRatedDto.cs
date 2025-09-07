namespace LoopUpAPI.DTOs
{
    public class RecentlyRatedDto
    {
        public string RatingId { get; set; } = null!;
        public string RatedUserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Image { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
