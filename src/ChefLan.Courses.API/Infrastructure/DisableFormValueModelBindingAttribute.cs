using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ChefLan.Courses.API.Infrastructure;

/// <summary>
/// Stops MVC from reading the multipart body into a form before the action runs. Without it, the
/// form value provider buffers the whole upload (to memory, then to a temp file on disk), which is
/// exactly what streaming a multi-gigabyte video must avoid. The action reads the request body
/// itself with MultipartReader.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var factories = context.ValueProviderFactories;
        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
