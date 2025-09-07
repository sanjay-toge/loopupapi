using LoopUpAPI.Helpers;
using LoopUpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using LoopUpAPI.DTOs;
using LoopUpAPI.Models;
namespace LoopUpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtHelper _jwtHelper;

    public AuthController(UserService userService, JwtHelper jwtHelper)
    {
        _userService = userService;
        _jwtHelper = jwtHelper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDto user)
    {
        Console.WriteLine($"Registering user: {user.Username}");
        var success = await _userService.RegisterAsync(user);
        if (!success) return BadRequest("User already exists");
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Login request)
    {
        var user = await _userService.AuthenticateAsync(request.Username, request.Password);
        if (user == null) return Unauthorized("Invalid credentials");

        var token = _jwtHelper.GenerateToken(user);
        return Ok(new { token });
    }
}
