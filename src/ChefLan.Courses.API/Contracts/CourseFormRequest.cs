using ChefLan.Domain.Enums;
using ChefLan.Logic.Courses.Contracts.Requests;

namespace ChefLan.Courses.API.Contracts;

/// <summary>
/// The multipart/form-data twin of <see cref="CourseRequest"/>: the same fields, plus a thumbnail that
/// is EITHER a URL (<c>thumbnailUrl</c>) or an uploaded file (<c>thumbnailFile</c>). Sending both is a
/// 400. <c>categoryIds</c> is a repeated field (one per id).
/// </summary>
public sealed class CourseFormRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? Description { get; set; }

    public string? WhatYouWillLearn { get; set; }

    public string? MetaDescription { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <summary>JPEG, PNG or WebP up to 5 MB. Uploaded to the CDN; its URL becomes the thumbnail.</summary>
    public IFormFile? ThumbnailFile { get; set; }

    public string? PromoVideoUrl { get; set; }

    public decimal PriceVnd { get; set; }

    public decimal? CompareAtPriceVnd { get; set; }

    public int? AccessDurationDays { get; set; }

    public string? LegacyFacebookGroupUrl { get; set; }

    public List<long> CategoryIds { get; set; } = [];

    public CourseLevel Level { get; set; } = CourseLevel.Unspecified;

    public CourseRequest ToRequest(string? thumbnailUrl) => new(
        Title,
        Subtitle,
        Description,
        WhatYouWillLearn,
        MetaDescription,
        thumbnailUrl,
        PromoVideoUrl,
        PriceVnd,
        CompareAtPriceVnd,
        AccessDurationDays,
        LegacyFacebookGroupUrl,
        CategoryIds,
        Level);
}
