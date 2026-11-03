using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using _2Korriku.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace _2Korriku.Services;

public sealed class HelpCatalog
{
    public IReadOnlyList<HelpModule> Modules { get; }
    public HelpCatalog(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Content", "Help", "manual.json");
        Modules = JsonSerializer.Deserialize<List<HelpModule>>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("Manuali nuk u lexua.");
        if (Modules.Select(m => m.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Modules.Count)
            throw new InvalidOperationException("Manuali përmban module të dyfishta.");
    }

    public HelpGuide Resolve(string? controller, string? action, string? view = null)
    {
        var module = Modules.FirstOrDefault(m => m.Key.Equals(controller, StringComparison.OrdinalIgnoreCase));
        if (module == null) return new("General", "Ndihmë", "Përdorimi i faqes",
            "Përdorni shpjegimin e fushave për formularin që keni hapur.",
            ["Kontrolloni titullin e faqes dhe regjistrimin që po përpunoni.", "Plotësoni fushat e shënuara si të detyrueshme.", "Lexoni mesazhin që shfaqet pas veprimit."],
            ["Udhëzuesi nuk plotëson dhe nuk dërgon formularë në emrin tuaj."]);
        var page = module.Pages.Keys.FirstOrDefault(p => p.Equals(action, StringComparison.OrdinalIgnoreCase))
            ?? module.Pages.Keys.FirstOrDefault(p => p.Equals(view, StringComparison.OrdinalIgnoreCase))
            ?? (module.Pages.ContainsKey("Index") ? "Index" : module.Pages.Keys.First());
        return Guide(module, page);
    }

    public HelpGuide Guide(HelpModule module, string page)
    {
        module.Details.TryGetValue(page, out var detail);
        return new(module.Key + "." + page, module.Title, module.Pages[page],
            string.IsNullOrWhiteSpace(detail?.Summary) ? module.Summary : detail.Summary,
            detail?.Steps.Count > 0 ? detail.Steps : module.Steps,
            module.Notes.Concat(detail?.Notes ?? []).Distinct().ToArray());
    }

    public async Task<List<HelpGuide>> AllowedAsync(ClaimsPrincipal user, IServiceProvider services)
    {
        var actions = services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
        var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = services.GetRequiredService<IAuthorizationService>();
        var result = new List<HelpGuide>();
        foreach (var module in Modules.Where(m => m.Key is not ("ParentPortal" or "ParentChat" or "Identity")))
        {
            if (module.Key is "Clubs" or "DataReset" && !services.GetRequiredService<ClubContext>().IsOwner) continue;
            var candidates = actions.Where(a => a.ControllerName == module.Key &&
                !(a.ActionConstraints?.OfType<HttpMethodActionConstraint>().Any(c => !c.HttpMethods.Contains("GET")) ?? false)).ToList();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in candidates)
            {
                var data = action.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAuthorizeData>()
                    .Concat(action.MethodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>()).ToArray();
                // Never promote anonymous routes to staff manual sections.
                if (data.Length == 0 || action.EndpointMetadata.OfType<IAllowAnonymous>().Any()) continue;
                var policy = await AuthorizationPolicy.CombineAsync(policyProvider, data);
                if (policy != null && (await authorization.AuthorizeAsync(user, null, policy)).Succeeded)
                    allowed.Add(action.ActionName);
            }
            foreach (var page in module.Pages.Keys.Where(allowed.Contains)) result.Add(Guide(module, page));
        }
        return result.OrderBy(g => g.Module).ThenBy(g => g.Title).ToList();
    }

    public static string Normalize(string text) => string.Concat(text.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    public static IReadOnlyList<HelpGuide> Search(IEnumerable<HelpGuide> guides, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return guides.ToList();
        var stop = new HashSet<string>(["si", "ta", "te", "me", "e", "i", "nje", "per", "mund", "mundem", "qe", "ne", "ka", "a", "qysh"]);
        var words = Normalize(query).Split([' ', '?', '.', ',', '!', ':'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !stop.Contains(w)).Distinct().ToList();
        if (words.Count == 0) return [];
        // Explicit synonyms keep search predictable; no generated or external answers.
        var synonyms = new Dictionary<string, string[]> {
            ["borxh"]=["borxh", "detyrim", "tarif"], ["fshij"]=["fshir", "fshi"],
            ["pages"]=["pages", "arket"], ["zbrit"]=["lirim", "korrigj"],
            ["femij"]=["lojtar", "familj"], ["formacion"]=["formacion", "titullar", "format"],
            ["password"]=["fjalekalim"], ["porosi"]=["dalj", "rekuizit"] };
        return guides.Select(g => {
            var title = Normalize(g.Title + " " + g.Module);
            var body = Normalize(g.Summary + " " + string.Join(" ", g.Steps.Concat(g.Notes)));
            var score = words.Sum(word => {
                var key = synonyms.Keys.FirstOrDefault(k => word.StartsWith(k));
                var terms = key == null ? new[]{word} : synonyms[key].Append(word);
                return terms.Max(t => title.Contains(t) ? 6 : body.Contains(t) ? 1 : 0);
            });
            return (Guide:g, Score:score);
        }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Guide.Title).Select(x => x.Guide).ToList();
    }
}
