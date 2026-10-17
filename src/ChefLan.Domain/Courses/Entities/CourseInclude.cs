namespace ChefLan.Domain.Courses.Entities;

/// <summary>
/// "Course P includes course C": an enrollment in P also unlocks C's included lessons, and
/// a customer who already has C gets <see cref="DiscountVnd"/> off P. One level only.
/// </summary>
public class CourseInclude
{
    /// <summary>The parent (P).</summary>
    public long CourseId { get; set; }

    /// <summary>The shared course (C).</summary>
    public long IncludedCourseId { get; set; }

    public decimal DiscountVnd { get; set; }

    public int SortOrder { get; set; }

    public Course? Course { get; set; }

    public Course? IncludedCourse { get; set; }
}
