namespace TrebaNam.API.Auth;

/// <summary>Admin prava drzime v konfiguracii (Admin:Emails), DB si ich len pamata.</summary>
public sealed class AdminEmails(IConfiguration configuration)
{
    private readonly HashSet<string> _emails = configuration
        .GetSection("Admin:Emails")
        .Get<string[]>()?
        .Where(e => !string.IsNullOrWhiteSpace(e))
        .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

    public bool Contains(string? email) => email is not null && _emails.Contains(email);
}
