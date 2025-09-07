using BCrypt.Net;
using LoopUpAPI.Models;
using MongoDB.Driver;
using LoopUpAPI.DTOs;

namespace LoopUpAPI.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
        // Ensure geospatial index exists for Location
        _users.Indexes.CreateOne(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Geo2DSphere(u => u.Location)
            )
        );
    }

    public async Task<List<User>> GetAllAsync() =>
        await _users.Find(_ => true).ToListAsync();

    public async Task<User?> GetByIdAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task CreateAsync(User user) =>
        await _users.InsertOneAsync(user);

    public async Task<bool> RegisterAsync(UserDto user)
    {
        var existingUser = await GetByUsernameAsync(user.Username);
        if (existingUser != null) return false;
        // user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
        var newUser = new User
        {
            Name = user.Name,
            Email = user.Email,
            Username = user.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
            Role = "User",

            CreatedAt = DateTime.UtcNow
        };
        await _users.InsertOneAsync(newUser);
        await _users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Geo2DSphere(u => u.Location)
            )
        );
        return true;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<bool> UpdateAsync(string id, UpdateUser updatedUser)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);

        // Only update allowed fields
        var update = Builders<User>.Update
            .Set(u => u.Name, updatedUser.Name)
            .Set(u => u.Bio, updatedUser.Bio)
            .Set(u => u.Age, updatedUser.Age)
            .Set(u => u.Image, updatedUser.Image);

        var result = await _users.UpdateOneAsync(filter, update);
        return result.MatchedCount > 0;
    }

}
