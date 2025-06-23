namespace Common.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string User = "User";
    }

    public static class Claims
    {
        public const string UserId = "userId";
        public const string Email = "email";
        public const string Role = "role";
    }

    public static class ValidationMessages
    {
        public const string Required = "This field is required.";
        public const string InvalidEmail = "Invalid email address.";
        public const string InvalidPhoneNumber = "Invalid phone number.";
        public const string PasswordTooShort = "Password must be at least 6 characters long.";
        public const string PasswordTooLong = "Password cannot exceed 100 characters.";
        public const string UserNotFound = "User not found.";
        public const string UserAlreadyExists = "User with this email already exists.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string AccountInactive = "User account is not active.";
    }

    public static class ResponseMessages
    {
        public const string Success = "Operation completed successfully.";
        public const string Created = "Resource created successfully.";
        public const string Updated = "Resource updated successfully.";
        public const string Deleted = "Resource deleted successfully.";
        public const string NotFound = "Resource not found.";
        public const string Unauthorized = "You are not authorized to perform this action.";
        public const string Forbidden = "Access denied.";
        public const string BadRequest = "Invalid request data.";
        public const string InternalServerError = "An internal server error occurred.";
    }

    public static class DefaultValues
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int DefaultRoleId = 2; // User role
        public const int TokenExpiryMinutes = 60;
    }
}