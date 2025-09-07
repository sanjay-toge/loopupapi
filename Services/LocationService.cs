using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using LoopUpAPI.Models;
using MongoDB.Driver.GeoJsonObjectModel;

namespace LoopUpAPI.Services;

public class LocationService
{
    private readonly IMongoCollection<User> _users;

    public LocationService(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<bool> UpdateLocationAsync(string userId, double latitude, double longitude)
    {
        var update = Builders<User>.Update
            .Set(u => u.Location, new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                new GeoJson2DGeographicCoordinates(longitude, latitude)
            ));
        var result = await _users.UpdateOneAsync(u => u.Id == userId, update);
        return result.ModifiedCount > 0;
    }

    public async Task<List<User>> GetNearbyUsersAsync(string currentUserId, double latitude, double longitude, double radiusInKm)
    {
        var nearFilter = Builders<User>.Filter.NearSphere(
            field: u => u.Location,
            point: new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                new GeoJson2DGeographicCoordinates(longitude, latitude)
            ),
            maxDistance: radiusInKm * 1000
        );

        // Exclude logged-in user
        var excludeSelf = Builders<User>.Filter.Ne(u => u.Id, currentUserId);

        var finalFilter = Builders<User>.Filter.And(nearFilter, excludeSelf);

        await UpdateLocationAsync(currentUserId, latitude, longitude);

        return await _users.Find(finalFilter).ToListAsync();
    }

}
