using Microsoft.AspNetCore.Mvc;
using LoopUpAPI.Models;
using LoopUpAPI.Services;
using LoopUpAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LoopUpAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendshipController : ControllerBase
{
    private readonly FriendshipService _friendshipService;

    public FriendshipController(FriendshipService friendshipRepo)
    {
        _friendshipService = friendshipRepo;
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendship dto)
    {
        // Extract user ID from JWT token claims
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(requesterId))
            return Unauthorized("User ID not found in token.");

        var result = await _friendshipService.SendFriendRequestAsync(requesterId, dto.RecipientId);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Friendship);
    }


    [HttpGet("status/{otherUserId}")]
    public async Task<IActionResult> GetStatus(string otherUserId)
    {
        // Extract user ID from JWT token claims
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(requesterId))
            return Unauthorized("User ID not found in token.");

        var status = await _friendshipService.GetFriendshipStatus(requesterId, otherUserId);
        return Ok((int)status); // send number for frontend enum
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateFriendRequest([FromBody] UpdateFriendship dto)
    {
        var recipientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(recipientId))
            return Unauthorized("Invalid token");

        if (!Enum.TryParse(dto.NewStatus, true, out FriendshipStatus status))
            return BadRequest("Invalid status value");

        var result = await _friendshipService.UpdateFriendRequestAsync(recipientId, dto.FriendshipId, status);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result.UpdatedFriendship);
    }


}