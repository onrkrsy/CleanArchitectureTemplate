using System.Data;
using Application.DTOs;
using Application.Services;
using Asp.Versioning;
using Common.Attributes;
using Common.Exceptions;
using Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CacheExampleController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICacheService _cacheService;
    private readonly ITransactionManager _transactionManager;
    private readonly IDbRepository _dbRepository;
    private readonly ILogger<CacheExampleController> _logger;

    public CacheExampleController(
        IUserService userService,
        ICacheService cacheService,
        ITransactionManager transactionManager,
        IDbRepository dbRepository,
        ILogger<CacheExampleController> logger)
    {
        _userService = userService;
        _cacheService = cacheService;
        _transactionManager = transactionManager;
        _dbRepository = dbRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets users with caching support
    /// </summary>
    /// <returns>Cached or fresh list of users</returns>
    [HttpGet("users-cached")]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetCachedUsers()
    {
        const string cacheKey = "all_users";
        const int cacheExpirationMinutes = 10;

        _logger.LogInformation("Attempting to get users from cache with key: {CacheKey}", cacheKey);

        var cachedUsers = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss - fetching users from database");
                var result = await _userService.GetAllUsersAsync();
                if (!result.IsSuccess)
                {
                    throw new BusinessException(result.Message ?? "Failed to fetch users");
                }
                return result.Data!;
            },
            TimeSpan.FromMinutes(cacheExpirationMinutes)
        );

        _logger.LogInformation("Returning {Count} users (cached: {IsCached})", 
            cachedUsers.Count(), await _cacheService.ExistsAsync(cacheKey));

        return Ok(cachedUsers);
    }

    /// <summary>
    /// Gets specific user with caching and demonstrates exception handling
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Cached or fresh user data</returns>
    [HttpGet("users-cached/{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetCachedUser(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("User ID must be greater than zero");
        }

        var cacheKey = $"user_{id}";
        _logger.LogInformation("Getting user {UserId} from cache", id);

        var user = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss - fetching user {UserId} from database", id);
                var result = await _userService.GetUserByIdAsync(id);
                if (!result.IsSuccess)
                {
                    throw new NotFoundException("User", id);
                }
                return result.Data!;
            },
            TimeSpan.FromMinutes(5)
        );

        return Ok(user);
    }

    /// <summary>
    /// Creates multiple users in a transaction with cache invalidation
    /// </summary>
    /// <param name="createUserDtos">List of users to create</param>
    /// <returns>Created users</returns>
    [HttpPost("users-bulk")]
    [Authorize(Roles = "Admin")]
    [Transactional(IsolationLevel.ReadCommitted, Timeout = 60)]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> CreateUsersBulk([FromBody] List<CreateUserDto> createUserDtos)
    {
        if (!createUserDtos.Any())
        {
            throw new ValidationException("At least one user must be provided");
        }

        if (createUserDtos.Count > 10)
        {
            throw new ValidationException("Cannot create more than 10 users at once");
        }

        _logger.LogInformation("Creating {Count} users in transaction", createUserDtos.Count);

        var createdUsers = await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var users = new List<UserResponseDto>();

            foreach (var userDto in createUserDtos)
            {
                var result = await _userService.CreateUserAsync(userDto);
                if (!result.IsSuccess)
                {
                    throw new BusinessException($"Failed to create user {userDto.Email}: {result.Message}");
                }
                users.Add(result.Data!);
            }

            // Invalidate cache after successful creation
            await _cacheService.RemoveByPatternAsync("user_*");
            await _cacheService.RemoveAsync("all_users");
            
            _logger.LogInformation("Successfully created {Count} users and invalidated cache", users.Count);
            return users;
        }, IsolationLevel.ReadCommitted, timeoutSeconds: 60);

        return CreatedAtAction(nameof(GetCachedUsers), createdUsers);
    }

    /// <summary>
    /// Updates user with cache refresh and transaction handling
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">Updated user data</param>
    /// <returns>Updated user</returns>
    [HttpPut("users-cached/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [Transactional]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> UpdateCachedUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (id <= 0)
        {
            throw new ValidationException("User ID must be greater than zero");
        }

        _logger.LogInformation("Updating user {UserId} with cache invalidation", id);

        var updatedUser = await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result.IsSuccess)
            {
                if (result.Message?.Contains("not found") == true)
                {
                    throw new NotFoundException("User", id);
                }
                throw new BusinessException(result.Message ?? "Failed to update user");
            }

            // Update cache with new data
            var cacheKey = $"user_{id}";
            await _cacheService.SetAsync(cacheKey, result.Data!, TimeSpan.FromMinutes(5));
            
            // Invalidate list cache
            await _cacheService.RemoveAsync("all_users");
            
            _logger.LogInformation("Updated user {UserId} and refreshed cache", id);
            return result.Data!;
        });

        return Ok(updatedUser);
    }

    /// <summary>
    /// Demonstrates exception handling with different exception types
    /// </summary>
    /// <param name="exceptionType">Type of exception to throw</param>
    /// <returns>Exception response</returns>
    [HttpPost("test-exception/{exceptionType}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> TestExceptionHandling(string exceptionType)
    {
        _logger.LogInformation("Testing exception handling for type: {ExceptionType}", exceptionType);

        await Task.Delay(100); // Simulate some work

        switch (exceptionType.ToLower())
        {
            case "business":
                throw new BusinessException("This is a business logic error example");
            
            case "validation":
                throw new ValidationException("Invalid input provided", "VALIDATION_FAILED");
            
            case "notfound":
                throw new NotFoundException("User", 999);
            
            case "conflict":
                throw new ConflictException("Email already exists in the system");
            
            case "unauthorized":
                throw new UnauthorizedException("You don't have permission to access this resource");
            
            case "forbidden":
                throw new ForbiddenException("This action is forbidden for your role");
            
            case "external":
                throw new ExternalServiceException("EmailService", "Failed to send notification email");
            
            case "generic":
                throw new InvalidOperationException("This is a generic system exception");
            
            default:
                throw new ArgumentException($"Unknown exception type: {exceptionType}");
        }
    }

    /// <summary>
    /// Demonstrates raw SQL with Dapper and transaction management
    /// </summary>
    /// <returns>User statistics from raw SQL</returns>
    [HttpGet("user-stats")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUserStatistics()
    {
        _logger.LogInformation("Getting user statistics using raw SQL");

        const string cacheKey = "user_statistics";

        var stats = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var sql = @"
                    SELECT 
                        COUNT(*) as TotalUsers,
                        COUNT(CASE WHEN IsActive = 1 THEN 1 END) as ActiveUsers,
                        COUNT(CASE WHEN IsActive = 0 THEN 1 END) as InactiveUsers,
                        COUNT(DISTINCT RoleId) as UniqueRoles
                    FROM Users";

                var result = await _dbRepository.QuerySingleOrDefaultAsync<dynamic>(sql);
                
                return new
                {
                    TotalUsers = result?.TotalUsers ?? 0,
                    ActiveUsers = result?.ActiveUsers ?? 0,
                    InactiveUsers = result?.InactiveUsers ?? 0,
                    UniqueRoles = result?.UniqueRoles ?? 0,
                    LastUpdated = DateTime.UtcNow
                };
            },
            TimeSpan.FromMinutes(15)
        );

        return Ok(stats);
    }

    /// <summary>
    /// Demonstrates complex transaction with rollback scenario
    /// </summary>
    /// <param name="shouldFail">Whether to simulate failure for rollback</param>
    /// <returns>Transaction result</returns>
    [HttpPost("transaction-demo")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> TransactionDemo([FromQuery] bool shouldFail = false)
    {
        _logger.LogInformation("Demonstrating transaction with shouldFail: {ShouldFail}", shouldFail);

        var result = await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            // Step 1: Create a test user
            var createUserDto = new CreateUserDto
            {
                FirstName = "Transaction",
                LastName = "Test",
                Email = $"transaction.test.{Guid.NewGuid()}@example.com",
                Password = "TestPassword123!",
                PhoneNumber = "+1234567890",
                RoleId = 3 // User role
            };

            var userResult = await _userService.CreateUserAsync(createUserDto);
            if (!userResult.IsSuccess)
            {
                throw new BusinessException("Failed to create test user");
            }

            var createdUser = userResult.Data!;
            _logger.LogInformation("Created test user with ID: {UserId}", createdUser.Id);

            // Step 2: Update the user
            var updateDto = new UpdateUserDto
            {
                FirstName = "Updated Transaction",
                LastName = "Test Updated",
                Email = createdUser.Email,
                PhoneNumber = "+9876543210",
                IsActive = true
            };

            var updateResult = await _userService.UpdateUserAsync(createdUser.Id, updateDto);
            if (!updateResult.IsSuccess)
            {
                throw new BusinessException("Failed to update test user");
            }

            _logger.LogInformation("Updated test user with ID: {UserId}", createdUser.Id);

            // Step 3: Simulate failure if requested
            if (shouldFail)
            {
                throw new BusinessException("Simulated failure - transaction should rollback");
            }

            // Step 4: Cache some data
            await _cacheService.SetAsync($"transaction_user_{createdUser.Id}", updateResult.Data!, TimeSpan.FromMinutes(5));

            return new
            {
                Message = "Transaction completed successfully",
                UserId = createdUser.Id,
                UserEmail = updateResult.Data!.Email,
                Steps = new[] { "User created", "User updated", "Data cached" }
            };
        }, IsolationLevel.ReadCommitted, timeoutSeconds: 30);

        return Ok(result);
    }

    /// <summary>
    /// Clear all user-related cache entries
    /// </summary>
    /// <returns>Cache clear result</returns>
    [HttpDelete("cache/users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearUserCache()
    {
        _logger.LogInformation("Clearing all user-related cache entries");

        await _cacheService.RemoveByPatternAsync("user_*");
        await _cacheService.RemoveAsync("all_users");
        await _cacheService.RemoveAsync("user_statistics");

        return Ok(new { Message = "User cache cleared successfully", ClearedAt = DateTime.UtcNow });
    }
}