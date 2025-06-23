using Application.DTOs;
using Application.Services;
using Asp.Versioning;
using Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Log(LogLevel.Information, LogParameters = true, LogExecutionTime = true)]
public class AttributeExampleController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AttributeExampleController> _logger;

    public AttributeExampleController(IUserService userService, ILogger<AttributeExampleController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets users with automatic caching (5 minutes)
    /// </summary>
    /// <returns>Cached list of users</returns>
    [HttpGet("users")]
    [Cache("all_users", ExpirationMinutes = 5)]
    [Log(LogLevel.Information, CustomMessage = "Fetching users with cache")]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsersWithCache()
    {
        // This method result will be automatically cached for 5 minutes
        var result = await _userService.GetAllUsersAsync();
        
        if (!result.IsSuccess)
            return StatusCode(500, result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Gets user by ID with automatic caching based on ID parameter
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Cached user data</returns>
    [HttpGet("users/{id}")]
    [Cache("user", ExpirationMinutes = 10, KeyParameters = new[] { "id" })]
    [Log(LogLevel.Information, SensitiveParameters = new[] { "id" })]
    [LogPerformance(SlowExecutionThresholdMs = 500)]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUserWithCache(int id)
    {
        // Cache key will be: "user_id_123" 
        var result = await _userService.GetUserByIdAsync(id);
        
        if (!result.IsSuccess)
            return NotFound(result.Message);
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a user and invalidates user cache
    /// </summary>
    /// <param name="createUserDto">User data</param>
    /// <returns>Created user</returns>
    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    [CacheEvict(KeyPatterns = new[] { "all_users", "user_*" })]
    [Log(LogLevel.Warning, SensitiveParameters = new[] { "password" })]
    [LogAudit("CREATE", "User", AuditParameters = new[] { "Email", "FirstName", "LastName" })]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> CreateUserWithCacheEviction([FromBody] CreateUserDto createUserDto)
    {
        // After successful execution, cache entries matching patterns will be evicted
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(createUserDto);
        
        if (!result.IsSuccess)
            return StatusCode(500, result.Message);
            
        return CreatedAtAction(nameof(GetUserWithCache), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates user and updates cache with new data
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">Updated user data</param>
    /// <returns>Updated user</returns>
    [HttpPut("users/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [CacheUpdate("user", KeyParameters = new[] { "id" }, ExpirationMinutes = 10)]
    [CacheEvict(KeyPatterns = new[] { "all_users" })]
    [Log(LogLevel.Information, IgnoreParameters = new[] { "updateUserDto" })]
    [LogAudit("UPDATE", "User", AuditParameters = new[] { "id" })]
    [LogPerformance(500, LogMemoryUsage = true)]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> UpdateUserWithCacheUpdate(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        // Result will update cache with key "user_id_123" and evict "all_users"
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.UpdateUserAsync(id, updateUserDto);
        
        if (!result.IsSuccess)
        {
            if (result.Message?.Contains("not found") == true)
                return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }
            
        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes user with comprehensive logging and cache eviction
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content</returns>
    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    [CacheEvict(KeyPatterns = new[] { "all_users", "user_{id}" })]
    [Log(LogLevel.Warning, LogOnlyOnError = false)]
    [LogAudit("DELETE", "User", AuditParameters = new[] { "id" }, LogIpAddress = true, LogUserAgent = true)]
    [LogPerformance(200)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserWithFullLogging(int id)
    {
        // Cache pattern "user_{id}" will be expanded to "user_123"
        var result = await _userService.DeleteUserAsync(id);
        
        if (!result.IsSuccess)
        {
            if (result.Message?.Contains("not found") == true)
                return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }
            
        return NoContent();
    }

    /// <summary>
    /// Example of different cache configurations
    /// </summary>
    /// <param name="category">Category filter</param>
    /// <param name="page">Page number</param>
    /// <returns>Filtered and cached results</returns>
    [HttpGet("users/filtered")]
    [Cache("filtered_users", ExpirationMinutes = 3, KeyParameters = new[] { "category", "page" }, 
           SlidingExpiration = false, IgnoreNullValues = false)]
    [Log(LogLevel.Debug, LogResponse = false, MaxResponseLength = 500)]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetFilteredUsersWithAdvancedCache(
        [FromQuery] string? category = null, 
        [FromQuery] int page = 1)
    {
        // Cache key: "filtered_users_category_active_page_1"
        // Fixed expiration (not sliding)
        // Will cache null values if IgnoreNullValues = false
        
        var result = await _userService.GetActiveUsersAsync();
        
        if (!result.IsSuccess)
            return StatusCode(500, result.Message);

        var filteredData = result.Data!.Skip((page - 1) * 10).Take(10);
        return Ok(filteredData);
    }

    /// <summary>
    /// Example of error logging and performance monitoring
    /// </summary>
    /// <param name="simulateError">Whether to simulate an error</param>
    /// <param name="simulateSlowness">Whether to simulate slow execution</param>
    /// <returns>Test result</returns>
    [HttpPost("test")]
    [Log(LogLevel.Information, LogOnlyOnError = true)]
    [LogPerformance(SlowExecutionThresholdMs = 100, LogCpuUsage = true, LogMemoryUsage = true)]
    [LogAudit("TEST", "System")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestLoggingAndPerformance(
        [FromQuery] bool simulateError = false,
        [FromQuery] bool simulateSlowness = false)
    {
        if (simulateSlowness)
        {
            await Task.Delay(200); // Will trigger slow execution log
        }

        if (simulateError)
        {
            throw new InvalidOperationException("Simulated error for testing");
        }

        return Ok(new 
        { 
            Message = "Test completed successfully",
            Timestamp = DateTime.UtcNow,
            RandomValue = Random.Shared.Next(1, 1000)
        });
    }

    /// <summary>
    /// Example of manual cache operations combined with attributes
    /// </summary>
    /// <returns>Complex cached result</returns>
    [HttpGet("complex-cache")]
    [Cache("complex_data", ExpirationMinutes = 15)]
    [Log(LogLevel.Information, CustomMessage = "Processing complex cache operation")]
    [LogPerformance(1000)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetComplexCachedData()
    {
        // Simulate complex processing
        await Task.Delay(50);
        
        var result = new
        {
            Users = await _userService.GetAllUsersAsync(),
            ActiveUsers = await _userService.GetActiveUsersAsync(),
            Timestamp = DateTime.UtcNow,
            ProcessingTime = DateTime.UtcNow.Millisecond
        };

        return Ok(result);
    }
}