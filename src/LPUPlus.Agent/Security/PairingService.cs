using System.Security.Cryptography;

namespace LPUPlus.Agent.Security;

/// <summary>
/// Generates cryptographically secure pairing codes for host-receiver pairing.
/// Format: XXXX-XXXX (e.g., "7K4P-92MX")
/// Uses only unambiguous uppercase alphanumeric characters (no 0/O, 1/I/L).
/// </summary>
public static class PairingCodeGenerator
{
    /// <summary>
    /// Characters used in pairing codes. Excludes ambiguous chars: 0, O, 1, I, L.
    /// </summary>
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>
    /// Generate a new pairing code (e.g., "7K4P-92MX").
    /// </summary>
    public static string Generate(int groupSize = 4, int groups = 2)
    {
        var totalChars = groupSize * groups;
        Span<char> code = stackalloc char[totalChars];

        for (var i = 0; i < totalChars; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        // Format as groups separated by dashes (e.g., "7K4P-92MX")
        var parts = new string[groups];
        for (var g = 0; g < groups; g++)
        {
            parts[g] = new string(code.Slice(g * groupSize, groupSize));
        }

        return string.Join('-', parts);
    }

    /// <summary>
    /// Compute HMAC-SHA256 hash of a pairing code for secure storage/comparison.
    /// The raw code is never stored — only its hash.
    /// </summary>
    public static string HashCode(string pairingCode, string secret)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        var codeBytes = System.Text.Encoding.UTF8.GetBytes(pairingCode.ToUpperInvariant().Replace("-", ""));

        var hash = HMACSHA256.HashData(keyBytes, codeBytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Normalize a pairing code (uppercase, strip dashes) for comparison.
    /// </summary>
    public static string Normalize(string pairingCode)
    {
        return pairingCode.ToUpperInvariant().Replace("-", "").Trim();
    }

    /// <summary>
    /// Validate pairing code format.
    /// </summary>
    public static bool IsValidFormat(string pairingCode)
    {
        var normalized = Normalize(pairingCode);
        if (normalized.Length != 8) return false;

        foreach (var c in normalized)
        {
            if (!Alphabet.Contains(c)) return false;
        }
        return true;
    }
}
