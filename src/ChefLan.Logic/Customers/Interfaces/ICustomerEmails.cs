using ChefLan.Logic.Abstractions;

namespace ChefLan.Logic.Customers.Interfaces;

/// <summary>
/// Builds the mails sent to customers. Templates and the public site's address are Infrastructure concerns;
/// Logic only hands over the data.
/// </summary>
public interface ICustomerEmails
{
    /// <summary>The invitation: a link that carries <paramref name="token"/>, valid until <paramref name="expiresAtUtc"/>.</summary>
    EmailMessage Invitation(string toEmail, string fullName, string token, DateTime expiresAtUtc);
}
