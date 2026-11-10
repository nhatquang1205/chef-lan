using ChefLan.Commons.Geography;
using ChefLan.Domain.Customers.Entities;
using ChefLan.Logic.Customers.Contracts;
using ChefLan.Logic.Customers.Mapping;
using ChefLan.Logic.Customers.Validators;

namespace ChefLan.Logic.Tests;

public class CustomerProfileTests
{
    private static readonly UpdateProfileRequestValidator ProfileValidator = new();
    private static readonly CustomerAdminRequestValidator AdminValidator = new();

    [Fact]
    public void There_are_34_unique_provinces_with_ascii_slug_codes()
    {
        Assert.Equal(34, VietnamProvinces.All.Count);
        Assert.Equal(34, VietnamProvinces.All.Select(p => p.Code).Distinct().Count());
        Assert.All(VietnamProvinces.All, p => Assert.Matches("^[a-z]+(-[a-z]+)*$", p.Code));
    }

    [Theory]
    [InlineData("ha-noi", true)]
    [InlineData("dak-lak", true)]
    [InlineData("Ha-Noi", false)]
    [InlineData("hanoi", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_accepts_only_known_codes(string? code, bool expected) =>
        Assert.Equal(expected, VietnamProvinces.IsValid(code));

    [Theory]
    [InlineData("0901234567")]
    [InlineData("+84 901 234 567")]
    [InlineData("090.123.4567")]
    public void Profile_accepts_valid_phones(string phone) =>
        Assert.True(ProfileValidator.Validate(new UpdateProfileRequest("Lan", phone, "ha-noi")).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567890123456")]
    [InlineData("09012x4567")]
    [InlineData("84+901234567")]
    public void Profile_rejects_bad_phones(string phone) =>
        Assert.False(ProfileValidator.Validate(new UpdateProfileRequest("Lan", phone, "ha-noi")).IsValid);

    [Theory]
    [InlineData("", "0901234567", "ha-noi")]
    [InlineData("Lan", "0901234567", "")]
    [InlineData("Lan", "0901234567", "atlantis")]
    public void Profile_requires_every_field(string name, string phone, string province) =>
        Assert.False(ProfileValidator.Validate(new UpdateProfileRequest(name, phone, province)).IsValid);

    [Fact]
    public void Admin_request_requires_email_and_a_known_province_when_given()
    {
        Assert.False(AdminValidator.Validate(new CustomerAdminRequest("Lan", null, null, null, null, null)).IsValid);
        Assert.False(AdminValidator.Validate(new CustomerAdminRequest("Lan", "a@example.com", null, "nope", null, null)).IsValid);
        Assert.True(AdminValidator.Validate(new CustomerAdminRequest("Lan", "a@example.com", null, null, null, null)).IsValid);
        Assert.True(AdminValidator.Validate(new CustomerAdminRequest("Lan", "a@example.com", null, "ha-noi", null, null)).IsValid);
    }

    [Theory]
    [InlineData("0901234567", "ha-noi", true)]
    [InlineData("0901234567", null, false)]
    [InlineData(null, "ha-noi", false)]
    [InlineData("  ", "ha-noi", false)]
    public void ProfileComplete_needs_phone_and_province(string? phone, string? province, bool expected)
    {
        var customer = new Customer { FullName = "Lan", Phone = phone, ProvinceCode = province };

        Assert.Equal(expected, customer.ToResponse().ProfileComplete);
    }
}
