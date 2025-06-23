using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserDto
{
    [Required]
    [StringLength(100, ErrorMessage = "First name must be between 1 and 100 characters", MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Last name must be between 1 and 100 characters", MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email must be between 1 and 255 characters", MinimumLength = 1)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number must be between 1 and 20 characters", MinimumLength = 1)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public int RoleId { get; set; } = 2; // Default to User role
}

public class UpdateUserDto
{
    [Required]
    [StringLength(100, ErrorMessage = "First name must be between 1 and 100 characters", MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Last name must be between 1 and 100 characters", MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email must be between 1 and 255 characters", MinimumLength = 1)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number must be between 1 and 20 characters", MinimumLength = 1)]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class LoginDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}