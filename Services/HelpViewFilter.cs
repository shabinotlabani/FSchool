using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _2Korriku.Services;

public sealed class HelpViewFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ViewResult view)
            view.ViewData["HelpView"] = Path.GetFileNameWithoutExtension(view.ViewName ?? "");
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
