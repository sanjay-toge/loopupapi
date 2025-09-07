
using MongoDB.Driver;
using LoopUpAPI.Models;
using LoopUpAPI.DTOs;
namespace LoopUpAPI.Services;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions; // For Expression<>
using System.Threading.Tasks;
public class FriendshipService
{
    private readonly IMongoCollection<Friendship> _friendships;

    public FriendshipService(IMongoDatabase database)
    {
        _friendships = database.GetCollection<Friendship>("Friendships");
    }

    public async Task InsertAsync(Friendship friendship) =>
        await _friendships.InsertOneAsync(friendship);

    public async Task<Friendship?> FindAsync(Expression<Func<Friendship, bool>> filter) =>
        await _friendships.Find(filter).FirstOrDefaultAsync();

    // public async Task<(bool Success, string ErrorMessage, Friendship? Friendship)> SendFriendRequestAsync(string requesterId, string recipientId)
    // {
    //     var existing = await _friendships
    //         .Find(f =>
    //             ((f.RequesterId == requesterId && f.RecipientId == recipientId) ||
    //             (f.RequesterId == recipientId && f.RecipientId == requesterId)) &&
    //             f.Status != FriendshipStatus.Declined
    //         )
    //         .FirstOrDefaultAsync();

    //     if (existing != null)
    //         return (false, "Friend request already exists", null);

    //     var friendship = new Friendship
    //     {
    //         RequesterId = requesterId,
    //         RecipientId = recipientId,
    //         Status = FriendshipStatus.Pending, // Use enum, not string
    //         CreatedAt = DateTime.UtcNow
    //     };

    //     await _friendships.InsertOneAsync(friendship);
    //     return (true, null, friendship);
    // }

    public async Task<(bool Success, string ErrorMessage, Friendship? Friendship)>
    SendFriendRequestAsync(string requesterId, string recipientId)
    {
        if (string.IsNullOrWhiteSpace(requesterId) || string.IsNullOrWhiteSpace(recipientId))
            return (false, "Requester and Recipient IDs must be provided.", null);

        if (requesterId == recipientId)
            return (false, "You cannot send a friend request to yourself.", null);

        // Check if a friendship already exists between these users
        var existingFriendship = await _friendships.Find(f =>
            (f.RequesterId == requesterId && f.RecipientId == recipientId) ||
            (f.RequesterId == recipientId && f.RecipientId == requesterId)
        ).FirstOrDefaultAsync();

        if (existingFriendship != null)
        {
            if (existingFriendship.Status == FriendshipStatus.Blocked)
                return (false, "The user is blocked. You cannot send a friend request.", null);

            if (existingFriendship.Status == FriendshipStatus.Declined)
                return (false, "The user has declined your previous request.", null);

            if (existingFriendship.Status == FriendshipStatus.Pending)
                return (false, "A friend request is already pending.", null);

            if (existingFriendship.Status == FriendshipStatus.Accepted)
                return (false, "You are already friends with this user.", null);
        }

        // Create a new pending friendship request
        var friendship = new Friendship
        {
            Id = Guid.NewGuid().ToString(),
            RequesterId = requesterId,
            RecipientId = recipientId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _friendships.InsertOneAsync(friendship);

        return (true, string.Empty, friendship);
    }


    // 🔹 NEW METHOD: Get Friendship Status
    public async Task<FriendshipStatus> GetFriendshipStatus(string userId, string otherUserId)
    {
        var friendship = await _friendships.Find(f =>
            (f.RequesterId == userId && f.RecipientId == otherUserId) ||
            (f.RequesterId == otherUserId && f.RecipientId == userId)
        ).FirstOrDefaultAsync();

        if (friendship == null)
            return FriendshipStatus.None;

        return friendship.Status;
    }

    public async Task<(bool Success, string ErrorMessage, Friendship? UpdatedFriendship)>
    UpdateFriendRequestAsync(string recipientId, string friendshipId, FriendshipStatus newStatus)
    {
        var friendship = await _friendships
            .Find(f => f.Id == friendshipId && f.RecipientId == recipientId)
            .FirstOrDefaultAsync();

        if (friendship == null)
            return (false, "Friend request not found or not yours to update", null);

        if (friendship.Status != FriendshipStatus.Pending)
            return (false, "Friend request is not pending", null);

        var update = Builders<Friendship>.Update
            .Set(f => f.Status, newStatus);

        var result = await _friendships.UpdateOneAsync(
            f => f.Id == friendshipId,
            update
        );

        if (result.ModifiedCount == 0)
            return (false, "Failed to update friend request", null);

        friendship.Status = newStatus;
        return (true, "", friendship);
    }


}



