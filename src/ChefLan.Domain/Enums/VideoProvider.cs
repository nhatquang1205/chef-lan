namespace ChefLan.Domain.Enums;

public enum VideoProvider
{
    None = 0,
    YouTube = 1,
    Vimeo = 2,
    Cdn = 3,
    Facebook = 4,

    /// <summary>
    /// Bunny Stream. VideoExternalId holds the Bunny video GUID, and the playback URL is
    /// a short-lived signed embed minted per request — never the stored column.
    /// </summary>
    Bunny = 5,
}
