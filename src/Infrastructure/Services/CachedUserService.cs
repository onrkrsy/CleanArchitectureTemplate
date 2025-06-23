using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Common.Attributes;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CachedUserService : IUserService
{
    private readonly IUserService _userService;

    public CachedUserService(IUserService userService)
    {
        _userService = userService;
    }

    [Cache("all_users", ExpirationMinutes = 10)]
    [Log(LogLevel.Information, CustomMessage = "Fetching all users with cache")]
    public virtual async Task<Result<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
    {
        return await _userService.GetAllUsersAsync();
    }

    [Cache("active_users", ExpirationMinutes = 5)]
    [Log(LogLevel.Information)]
    public virtual async Task<Result<IEnumerable<UserResponseDto>>> GetActiveUsersAsync()
    {
        return await _userService.GetActiveUsersAsync();
    }

    [Cache("user", KeyParameters = new[] { "id" }, ExpirationMinutes = 15)]
    [Log(LogLevel.Information, LogParameters = true)]
    [LogPerformance(500)]
    public virtual async Task<Result<UserResponseDto>> GetUserByIdAsync(int id)
    {
        return await _userService.GetUserByIdAsync(id);
    }

    [Cache("user_by_email", KeyParameters = new[] { "email" }, ExpirationMinutes = 10)]
    [Log(LogLevel.Information, SensitiveParameters = new[] { "email" })]
    public virtual async Task<Result<UserResponseDto>> GetUserByEmailAsync(string email)
    {
        return await _userService.GetUserByEmailAsync(email);
    }

    [CacheEvict(KeyPatterns = new[] { "all_users", "active_users" })]
    [Log(LogLevel.Warning, SensitiveParameters = new[] { "Password" })]
    [LogAudit("CREATE", "User", AuditParameters = new[] { "Email", "FirstName", "LastName" })]
    [LogPerformance(1000)]
    public virtual async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        return await _userService.CreateUserAsync(createUserDto);
    }

    [CacheUpdate("user", KeyParameters = new[] { "id" })]
    [CacheEvict(KeyPatterns = new[] { "all_users", "active_users" })]
    [Log(LogLevel.Information, LogParameters = true)]
    [LogAudit("UPDATE", "User", AuditParameters = new[] { "id" })]
    public virtual async Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        return await _userService.UpdateUserAsync(id, updateUserDto);
    }

    [CacheEvict(KeyPatterns = new[] { "all_users", "active_users", "user_{id}", "user_by_email_*" })]
    [Log(LogLevel.Warning, LogParameters = true)]
    [LogAudit("DELETE", "User", AuditParameters = new[] { "id" }, LogIpAddress = true)]
    public virtual async Task<Result> DeleteUserAsync(int id)
    {
        return await _userService.DeleteUserAsync(id);
    }

    [Log(LogLevel.Information, SensitiveParameters = new[] { "Password" })]
    [LogAudit("LOGIN", "Authentication", AuditParameters = new[] { "Email" }, LogIpAddress = true, LogUserAgent = true)]
    [LogPerformance(2000)]
    public virtual async Task<Result<LoginResponseDto>> LoginAsync(LoginDto loginDto)
    {
        return await _userService.LoginAsync(loginDto);
    }
}