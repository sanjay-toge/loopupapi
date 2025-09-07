using LoopUpAPI.Helpers;
using LoopUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using LoopUpAPI.DTOs;
using LoopUpAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LoopUpAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly LocationService _locationService;

    public LocationController(LocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateLocation([FromBody] Location dto)
    {
        var userId = User.FindFirst("id")?.Value;
        var success = await _locationService.UpdateLocationAsync(userId, dto.Latitude, dto.Longitude);
        if (!success) return BadRequest("Failed to update location.");
        return Ok("Location updated successfully.");
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby(double latitude, double longitude, double radiusKm = 5)
    {
        var currentUserId = User.FindFirst("id")?.Value;
        var users = await _locationService.GetNearbyUsersAsync(currentUserId, latitude, longitude, radiusKm);
        return Ok(users);
    }
}

