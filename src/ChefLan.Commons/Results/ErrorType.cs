namespace ChefLan.Commons.Results;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5,
    Unexpected = 6,

    /// <summary>A feature that is not configured on this deployment (503).</summary>
    Unavailable = 7,

    /// <summary>An external service we depend on failed (502).</summary>
    Upstream = 8,
}
