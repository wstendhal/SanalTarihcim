using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SanalTarihcim.Pages.Admin
{
    public class LoginModel(IConfiguration configuration) : PageModel
    {
        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; private set; }

        public IActionResult OnPost()
        {
            var adminEmail = configuration["Admin:Email"];
            var adminPassword = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                ErrorMessage = "Admin girişi henüz yapılandırılmamış.";
                return Page();
            }

            var passwordMatches = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Password),
                Encoding.UTF8.GetBytes(adminPassword));

            if (string.Equals(Email.Trim(), adminEmail, StringComparison.OrdinalIgnoreCase) && passwordMatches)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("AdminEmail", adminEmail);

                return Redirect("/Admin");
            }

            ErrorMessage = "Geçersiz admin bilgileri.";
            return Page();
        }
    }
}
