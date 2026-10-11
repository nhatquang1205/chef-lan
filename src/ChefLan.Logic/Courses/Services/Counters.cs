using ChefLan.Domain.Courses.Entities;

namespace ChefLan.Logic.Courses.Services;

/// <summary>
/// Denormalized counters, recomputed in memory from tracked entities so they land in the
/// same SaveChanges (one transaction) as the write that changed them. Published, live rows
/// only — callers pass the live set with any row being removed already excluded.
/// </summary>
internal static class Counters
{
    public static void Apply(Lesson lesson, IEnumerable<LessonVideo> liveVideos)
    {
        var published = liveVideos.Where(v => v.IsPublished).ToList();
        lesson.VideoCount = published.Count;
        lesson.DurationSeconds = published.Sum(v => v.DurationSeconds);
    }

    public static void Apply(Course course, IEnumerable<Lesson> liveLessons)
    {
        var published = liveLessons.Where(l => l.IsPublished).ToList();
        course.LessonCount = published.Count;
        course.TotalDurationSeconds = published.Sum(l => l.DurationSeconds);
    }

    /// <summary>The requested ids must be exactly the current ids, each once.</summary>
    public static bool IsExactOrdering(IReadOnlyList<long> requested, IReadOnlyCollection<long> current) =>
        requested.Count == current.Count
        && requested.Distinct().Count() == requested.Count
        && requested.ToHashSet().SetEquals(current);
}
