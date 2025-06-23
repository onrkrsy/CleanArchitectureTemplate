using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using AutoMapper;
using Common.Constants;
using Common.Models;
using Common.Utilities;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAppUnitOfWork unitOfWork,
        IMapper mapper,
        IAuthService authService,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _authService = authService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all users");
            var users = await _unitOfWork.Users.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);
            return Result<IEnumerable<UserResponseDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching users");
            return Result<IEnumerable<UserResponseDto>>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<UserResponseDto>>> GetActiveUsersAsync()
    {
        try
        {
            _logger.LogInformation("Fetching active users");
            var users = await _unitOfWork.Users.GetActiveUsersAsync();
            var userDtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);
            return Result<IEnumerable<UserResponseDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching active users");
            return Result<IEnumerable<UserResponseDto>>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<UserResponseDto>> GetUserByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Fetching user with ID: {UserId}", id);
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return Result<UserResponseDto>.Failure(AppConstants.ValidationMessages.UserNotFound);
            }

            var userDto = _mapper.Map<UserResponseDto>(user);
            return Result<UserResponseDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with ID: {UserId}", id);
            return Result<UserResponseDto>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<UserResponseDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            _logger.LogInformation("Fetching user with email: {Email}", email);
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found", email);
                return Result<UserResponseDto>.Failure(AppConstants.ValidationMessages.UserNotFound);
            }

            var userDto = _mapper.Map<UserResponseDto>(user);
            return Result<UserResponseDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with email: {Email}", email);
            return Result<UserResponseDto>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            _logger.LogInformation("Creating new user with email: {Email}", createUserDto.Email);

            // Check if user with email already exists
            var emailExists = await _unitOfWork.Users.IsEmailExistsAsync(createUserDto.Email);
            if (emailExists)
            {
                _logger.LogWarning("User with email {Email} already exists", createUserDto.Email);
                return Result<UserResponseDto>.Failure(AppConstants.ValidationMessages.UserAlreadyExists);
            }

            var user = _mapper.Map<User>(createUserDto);
            user.PasswordHash = PasswordHasher.HashPassword(createUserDto.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

            var userDto = _mapper.Map<UserResponseDto>(user);
            return Result<UserResponseDto>.Success(userDto, AppConstants.ResponseMessages.Created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user");
            return Result<UserResponseDto>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        try
        {
            _logger.LogInformation("Updating user with ID: {UserId}", id);

            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return Result<UserResponseDto>.Failure(AppConstants.ValidationMessages.UserNotFound);
            }

            // Check if email is being changed and if new email already exists
            if (user.Email != updateUserDto.Email)
            {
                var emailExists = await _unitOfWork.Users.IsEmailExistsAsync(updateUserDto.Email);
                if (emailExists)
                {
                    _logger.LogWarning("User with email {Email} already exists", updateUserDto.Email);
                    return Result<UserResponseDto>.Failure(AppConstants.ValidationMessages.UserAlreadyExists);
                }
            }

            _mapper.Map(updateUserDto, user);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User with ID {UserId} updated successfully", id);

            var userDto = _mapper.Map<UserResponseDto>(user);
            return Result<UserResponseDto>.Success(userDto, AppConstants.ResponseMessages.Updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {UserId}", id);
            return Result<UserResponseDto>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result> DeleteUserAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting user with ID: {UserId}", id);

            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return Result.Failure(AppConstants.ValidationMessages.UserNotFound);
            }

            _unitOfWork.Users.Delete(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User with ID {UserId} deleted successfully", id);

            return Result.Success(AppConstants.ResponseMessages.Deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}", id);
            return Result.Failure(AppConstants.ResponseMessages.InternalServerError);
        }
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Attempting to authenticate user with email: {Email}", loginDto.Email);

            var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
                return Result<LoginResponseDto>.Failure(AppConstants.ValidationMessages.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Email}", loginDto.Email);
                return Result<LoginResponseDto>.Failure(AppConstants.ValidationMessages.AccountInactive);
            }

            if (!PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password attempt for user: {Email}", loginDto.Email);
                return Result<LoginResponseDto>.Failure(AppConstants.ValidationMessages.InvalidCredentials);
            }

            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Role.Name);
            var userDto = _mapper.Map<UserResponseDto>(user);

            _logger.LogInformation("User authenticated successfully: {Email}", loginDto.Email);

            var loginResponse = new LoginResponseDto
            {
                Token = token,
                User = userDto
            };

            return Result<LoginResponseDto>.Success(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while authenticating user with email: {Email}", loginDto.Email);
            return Result<LoginResponseDto>.Failure(
                AppConstants.ResponseMessages.InternalServerError);
        }
    }
}