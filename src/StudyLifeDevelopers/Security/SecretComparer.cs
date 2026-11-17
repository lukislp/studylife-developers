using System.Security.Cryptography;
using System.Text;

namespace StudyLifeDevelopers.Security;

/// <summary>
/// Constant-time comparison for shared secrets and other attacker-supplied tokens. Both values
/// are hashed with SHA-256 first, so the comparison runs over two fixed 32-byte buffers: neither
/// the position of the first differing character nor the length of the expected value leaks
/// through the response time. A plain <c>==</c> on strings stops at the first mismatch.
/// </summary>
public static class SecretComparer
{
    /// <summary>True only when <paramref name="expected"/> is non-empty and
    /// <paramref name="provided"/> equals it. An empty or missing expected value never matches,
    /// so an unconfigured secret cannot be satisfied by an empty header.</summary>
    public static bool FixedTimeEquals(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(expected) || provided is null)
            return false;
        Span<byte> left = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> right = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(provided), left);
        SHA256.HashData(Encoding.UTF8.GetBytes(expected), right);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
