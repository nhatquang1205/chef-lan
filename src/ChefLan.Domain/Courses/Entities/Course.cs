using ChefLan.Domain.Common;
using ChefLan.Domain.Enums;

namespace ChefLan.Domain.Courses.Entities;

public class Course : AuditableSoftDeletableEntity, IHasRowVersion
{
    public required string Slug { get; set; }

    public required string Title { get; set; }

    public string? Subtitle { get; set; }

    public string? Description { get; set; }

    public string? WhatYouWillLearn { get; set; }

    public string? MetaDescription { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? PromoVideoUrl { get; set; }

    public CourseLevel Level { get; set; } = CourseLevel.Unspecified;

    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    public DateTime? PublishedAt { get; set; }

    public decimal PriceVnd { get; set; }

    public decimal? CompareAtPriceVnd { get; set; }

    public string Currency { get; set; } = "VND";

    /// <summary>Null means lifetime access.</summary>
    public int? AccessDurationDays { get; set; }

    public int LessonCount { get; set; }

    public int TotalDurationSeconds { get; set; }

    public int EnrollmentCount { get; set; }

    /// <summary>Bridge to the Facebook group this course used to be sold through.</summary>
    public string? LegacyFacebookGroupUrl { get; set; }

    public uint RowVersion { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = [];

    public ICollection<CourseCategory> CourseCategories { get; set; } = [];

    public ICollection<Enrollment> Enrollments { get; set; } = [];

    /// <summary>Courses this one includes (this course is the parent).</summary>
    public ICollection<CourseInclude> Includes { get; set; } = [];

    /// <summary>Courses that include this one (this course is the shared child).</summary>
    public ICollection<CourseInclude> IncludedIn { get; set; } = [];
}
