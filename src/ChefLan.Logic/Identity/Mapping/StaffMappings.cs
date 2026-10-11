using ChefLan.Domain.Identity.Entities;
using ChefLan.Logic.Identity.Contracts;

namespace ChefLan.Logic.Identity.Mapping;

internal static class StaffMappings
{
    public static StaffResponse ToResponse(this User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role.ToString(),
        user.JobTitle,
        user.MustChangePassword);
}
