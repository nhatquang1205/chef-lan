namespace ChefLan.Commons.Auth;

/// <summary>
/// Which population a JWT belongs to, from the "act" claim. Never persisted. Lives in
/// Commons because <see cref="ICurrentUser"/> exposes it and Commons cannot reference Domain.
/// </summary>
public enum ActorType
{
    Staff = 1,
    Customer = 2,
}
