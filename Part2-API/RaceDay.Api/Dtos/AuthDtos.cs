using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Details needed to create a new account.</summary>
public class RegisterRequest
{
    /// <summary>The user's full name.</summary>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Must be unique. Stored in lower case.</summary>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters. It is hashed before it is stored and is never returned.</summary>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Optional contact number.</summary>
    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}

/// <summary>Credentials used to log in.</summary>
public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Public details of a user. The password hash is never included.</summary>
public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static UserResponse From(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}

/// <summary>Details a user may change on their own profile.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }
}

/// <summary>Simple confirmation message.</summary>
public class MessageResponse
{
    public MessageResponse(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}
