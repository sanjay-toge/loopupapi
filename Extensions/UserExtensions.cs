using LoopUpAPI.Models;
using LoopUpAPI.DTOs;
namespace LoopUpAPI.Extensions
{
    public static class UserExtensions
    {
        public static UserDto ToDto(this User user)
        {
            return new UserDto
            {
                Id = user.Id!,
                Name = user.Name,
                Email = user.Email,
                Rating = user.Rating ?? 0,
                Bio = user.Bio,
                Age = user.Age,
                Image = user.Image,
                Username = user.Username,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
