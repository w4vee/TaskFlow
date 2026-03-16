namespace TaskFlow.Application.Interfaces;

/// <summary>
/// Interface for password hashing.
/// Why interface? So we can swap BCrypt for Argon2 later without changing Application code.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
