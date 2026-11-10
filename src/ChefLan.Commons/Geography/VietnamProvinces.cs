namespace ChefLan.Commons.Geography;

public sealed record Province(string Code, string Name);

/// <summary>
/// The 34 provincial-level units of Vietnam in force since 1 July 2025 (Resolution 202/2025/QH15):
/// 6 cities ("TP. ...") and 28 provinces. Codes are lowercase ASCII slugs and are stored, so never rename one.
/// Listed in Vietnamese alphabetical order of the name without the "TP." prefix.
/// </summary>
public static class VietnamProvinces
{
    public static IReadOnlyList<Province> All { get; } =
    [
        new("an-giang", "An Giang"),
        new("bac-ninh", "Bắc Ninh"),
        new("ca-mau", "Cà Mau"),
        new("cao-bang", "Cao Bằng"),
        new("can-tho", "TP. Cần Thơ"),
        new("da-nang", "TP. Đà Nẵng"),
        new("dak-lak", "Đắk Lắk"),
        new("dien-bien", "Điện Biên"),
        new("dong-nai", "Đồng Nai"),
        new("dong-thap", "Đồng Tháp"),
        new("gia-lai", "Gia Lai"),
        new("ha-noi", "TP. Hà Nội"),
        new("ha-tinh", "Hà Tĩnh"),
        new("hai-phong", "TP. Hải Phòng"),
        new("ho-chi-minh", "TP. Hồ Chí Minh"),
        new("hue", "TP. Huế"),
        new("hung-yen", "Hưng Yên"),
        new("khanh-hoa", "Khánh Hòa"),
        new("lai-chau", "Lai Châu"),
        new("lang-son", "Lạng Sơn"),
        new("lao-cai", "Lào Cai"),
        new("lam-dong", "Lâm Đồng"),
        new("nghe-an", "Nghệ An"),
        new("ninh-binh", "Ninh Bình"),
        new("phu-tho", "Phú Thọ"),
        new("quang-ngai", "Quảng Ngãi"),
        new("quang-ninh", "Quảng Ninh"),
        new("quang-tri", "Quảng Trị"),
        new("son-la", "Sơn La"),
        new("tay-ninh", "Tây Ninh"),
        new("thai-nguyen", "Thái Nguyên"),
        new("thanh-hoa", "Thanh Hóa"),
        new("tuyen-quang", "Tuyên Quang"),
        new("vinh-long", "Vĩnh Long"),
    ];

    private static readonly HashSet<string> Codes = All.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);

    public static bool IsValid(string? code) => code is not null && Codes.Contains(code);
}
