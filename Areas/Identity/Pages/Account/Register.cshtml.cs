using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _2Korriku.Areas.Identity.Pages.Account;

// Staff accounts are created by an administrator through Users, never publicly.
[AllowAnonymous]
public class RegisterModel : PageModel
{
    public IActionResult OnGet() => NotFound();
    public IActionResult OnPost() => NotFound();
}
