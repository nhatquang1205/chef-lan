namespace ChefLan.Domain.Courses.Entities;

/// <summary>Join entity: one course legitimately belongs to several categories.</summary>
public class CourseCategory
{
    public long CourseId { get; set; }

    public long CategoryId { get; set; }

    public Course? Course { get; set; }

    public Category? Category { get; set; }
}
