using System.ComponentModel.DataAnnotations;
using _2Korriku.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _2Korriku.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";

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

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
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

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Llogaria është bllokuar përkohësisht. Provoni përsëri më vonë."
            : "Emaili ose fjalëkalimi nuk është i saktë.");
        return Page();
    }
}
