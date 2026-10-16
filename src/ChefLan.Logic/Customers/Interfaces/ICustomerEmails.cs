using ChefLan.Logic.Abstractions;

namespace ChefLan.Logic.Customers.Interfaces;

/// <summary>Builds the mails sent to customers. Templates are an Infrastructure concern; Logic only asks for a message.</summary>
public interface ICustomerEmails
{
    /// <summary>The invitation: a link to set a password, valid until <paramref name="expiresAtUtc"/>.</summary>
    EmailMessage Invitation(string toEmail, string fullName, string link, DateTime expiresAtUtc);
}
