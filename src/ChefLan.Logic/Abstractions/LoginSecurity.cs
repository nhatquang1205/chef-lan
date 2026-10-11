namespace ChefLan.Logic.Abstractions;

/// <summary>Rules shared by both login flows.</summary>
public static class LoginSecurity
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// A valid work-factor-12 BCrypt hash of a random string nobody knows. Verified against
    /// when the email is unknown, so "no such account" costs the same time as "wrong
    /// password" — otherwise the clock answers the enumeration question.
    /// </summary>
    public const string DummyPasswordHash =
        "$2a$12$gxW./ey/8cMwgenwvr1UKuNxpQDkQ2CWOEpUIuUqGYHLy2ZoUZS4y";
}
