using Microsoft.AspNetCore.Mvc;
using LoopUpAPI.Models;
using LoopUpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using LoopUpAPI.DTOs;
using LoopUpAPI.Mappers;
using LoopUpAPI.Extensions;

namespace LoopUpAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _service;

    public UserController(UserService service)
    {
        _service = service;
    }

    // [HttpPost]
    // public async Task<IActionResult> Create(User user)
    // {
    //     await _service.CreateAsync(user);
    //     return Ok(user);
    // }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _service.GetAllAsync();
        var userDtos = users.Select(u => u.ToDto()).ToList();
        if (userDtos == null)
            return NotFound();

        return Ok(userDtos);
        // return await _service.GetAllAsync();
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        // var user = await _service.GetByIdAsync(id);
        // if (user == null) return NotFound();
        // return user;
        var user = await _service.GetByIdAsync(id);

        if (user == null)
            return NotFound();

        return Ok(user.ToDto()); // mapping happens here
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUser user)
    {
        var success = await _service.UpdateAsync(id, user);

        if (!success)
            return NotFound(new { message = "User not found" });

        return Ok();
    }

}
