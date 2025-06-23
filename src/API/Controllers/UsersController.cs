using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet]
    //[Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var result = await _userService.GetAllUsersAsync();
        
        if (!result.IsSuccess)
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets active users only
    /// </summary>
    /// <returns>List of active users</returns>
    [HttpGet("active")]
    //[Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetActiveUsers()
    {
        var result = await _userService.GetActiveUsersAsync();
        
        if (!result.IsSuccess)
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a user by email
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User details</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUserByEmail(string email)
    {
        var result = await _userService.GetUserByEmailAsync(email);
        
        if (!result.IsSuccess)
            return NotFound(result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new user (Admin only)
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(createUserDto);
        
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("already exists"))
                return Conflict(result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        }
            
        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">User update data</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("not found"))
                return NotFound(result.Message);
            if (result.Message.Contains("already exists"))
                return Conflict(result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        }
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("not found"))
                return NotFound(result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        }
            
        return NoContent();
    }

    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.LoginAsync(loginDto);
        
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("Invalid") || result.Message.Contains("not active"))
                return Unauthorized(result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, result.Message);
        }
            
        return Ok(result.Data);
    }
}