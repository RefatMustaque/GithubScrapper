using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace GithubScrapper.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = @"/Account/Callback" }, "GitHub");
        }

        //public IActionResult Logout()
        //{
        //    return SignOut(new AuthenticationProperties { RedirectUri = @"/" }, CookieAuthenticationDefaults.AuthenticationScheme);
        //}

        // Logout action
        public async Task<IActionResult> Logout()
        {
            // 1. Sign out from the cookie-based authentication scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Optionally, remove GitHub-related claims if needed (this is optional)
            // This step is not strictly necessary but may be useful in some cases.
            var identity = (ClaimsIdentity)User.Identity;
            var githubClaim = identity.Claims.First(c => c.Type == "urn:github:login");
            if (githubClaim != null)
            {
                identity.RemoveClaim(githubClaim);
            }

            // 3. Explicitly delete cookies related to GitHub OAuth session (if any)
            var cookieName = HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme).Cookie.Name;

            Response.Cookies.Delete(cookieName, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/"
            });

            // 4. Clear any session data (optional)
            HttpContext.Session.Clear();

            // 5. Redirect to the home page or login page
            return RedirectToAction("Index", "Home");
        }


        public async Task<IActionResult> Callback()
        {
            // This method will be triggered by GitHub's redirection after login
            var authenticateResult = await HttpContext.AuthenticateAsync();

            if (!authenticateResult.Succeeded)
            {
                // Authentication failed; redirect to a failure page
                return RedirectToAction("LoginFailure");
            }

            // Authentication succeeded, process user data if necessary
            var userName = authenticateResult.Principal.Identity.Name;

            // You could store the user information here in your session or database

            // Redirect to a page after successful login
            return RedirectToAction("Index", "Home");
        }

        public IActionResult LoginFailure()
        {
            return View("LoginFailure");
        }
    }
}
