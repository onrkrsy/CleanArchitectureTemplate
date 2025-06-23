using Application.DTOs;
using Common.Models;

namespace Application.Services;

public interface IUserService
{
    Task<Result<IEnumerable<UserResponseDto>>> GetAllUsersAsync();
    Task<Result<IEnumerable<UserResponseDto>>> GetActiveUsersAsync();
    Task<Result<UserResponseDto>> GetUserByIdAsync(int id);
    Task<Result<UserResponseDto>> GetUserByEmailAsync(string email);
    Task<Result<UserResponseDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<Result> DeleteUserAsync(int id);
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto loginDto);
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserResponseDto User { get; set; } = null!;
}