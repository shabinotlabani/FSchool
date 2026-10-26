using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _2Korriku.Services;

namespace _2Korriku.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";
    public string Language { get; private set; } = "sq";
    public string T(string key) => LoginLanguage.Text(Language,key);
    private void SelectLanguage(string? language)
    {
        Language = LoginLanguage.Normalize(language ?? Request.Cookies[LoginLanguage.Cookie]);
        if (language != null && LoginLanguage.Codes.Contains(language))
            Response.Cookies.Append(LoginLanguage.Cookie, Language, new CookieOptions {
                HttpOnly=true, Secure=Request.IsHttps, SameSite=SameSiteMode.Lax, IsEssential=true,
                Path=Request.PathBase.HasValue?Request.PathBase.Value:"/", MaxAge=TimeSpan.FromDays(365)
            });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Shkruani adresën e emailit.")]
        [EmailAddress(ErrorMessage = "Shkruani një adresë emaili të vlefshme.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shkruani fjalëkalimin.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null, string? language = null)
    {
        SelectLanguage(language);
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null, string? language = null)
    {
        SelectLanguage(language);
        foreach(var entry in ModelState.Values)
            for(var i=0;i<entry.Errors.Count;i++) {
                var key=entry.Errors[i].ErrorMessage switch {
                    "Shkruani adresën e emailit." => "EmailRequired",
                    "Shkruani një adresë emaili të vlefshme." => "EmailInvalid",
                    "Shkruani fjalëkalimin." => "PasswordRequired", _ => null
                };
                if(key!=null) entry.Errors[i]=new Microsoft.AspNetCore.Mvc.ModelBinding.ModelError(T(key));
            }
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(
            Input.Email.Trim(), Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) {
            var user = await signInManager.UserManager.FindByEmailAsync(Input.Email.Trim());
            Response.Cookies.Delete(_2Korriku.Services.ClubContext.CookieName, new CookieOptions { Path = Request.PathBase.HasValue ? Request.PathBase.Value : "/" });
            if (user?.IsPlatformOwner == true) return LocalRedirect(Url.Content("~/Clubs"));
            if (user != null && await signInManager.UserManager.IsInRoleAsync(user, Roles.FieldOperator))
                return LocalRedirect(Url.Content("~/Fields"));
            return LocalRedirect(ReturnUrl);
        }
        if (result.RequiresTwoFactor)
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl, RememberMe = Input.RememberMe });

        ModelState.AddModelError(string.Empty, T(result.IsLockedOut ? "Locked" : "Invalid"));
        return Page();
    }
}
