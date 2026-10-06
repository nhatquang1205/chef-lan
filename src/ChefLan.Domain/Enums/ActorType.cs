namespace ChefLan.Domain.Enums;

/// <summary>
/// Which population a JWT belongs to. Carried in the "act" claim and never persisted.
/// Exists because "sub" means a user id under the Internal scheme and a customer id
/// under the Customer scheme.
/// </summary>
public enum ActorType
{
    Staff = 1,
    Customer = 2,
}
