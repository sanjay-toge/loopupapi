using LoopUpAPI.Models;
using LoopUpAPI.DTOs;

namespace LoopUpAPI.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToDto(User user)
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
