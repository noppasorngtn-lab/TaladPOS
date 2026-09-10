namespace TaladPOS.Domain.Entities;

/// <summary>A user account that can log in to the sales screen and, for Admins, back-office screens.</summary>
public class Staff
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public StaffRole Role { get; private set; }
    public bool IsActive { get; private set; }

    private Staff()
    {
    }

    public Staff(string name, string username, string passwordHash, StaffRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Staff name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        Name = name;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }
}
