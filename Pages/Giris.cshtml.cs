using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages
{
    public class GirisModel : PageModel
    {
        private readonly AuthService _authService;

        public GirisModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Parola { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EmailQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Message { get; set; }

        public string? InfoMessage { get; private set; }

        public void OnGet()
        {
            if (!string.IsNullOrWhiteSpace(EmailQuery))
            {
                Email = EmailQuery;
            }

            if (!string.IsNullOrWhiteSpace(Message))
            {
                InfoMessage = Message;
            }
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Parola))
            {
                InfoMessage = "E-posta ve şifre zorunludur.";
                return Page();
            }

            var loginResult = _authService.Login(Email, Parola);
            if (!loginResult.Success)
            {
                InfoMessage = loginResult.Message;
                return Page();
            }

            HttpContext.Session.SetString("IsLoggedIn", "true");
            HttpContext.Session.SetString("UserEmail", Email);

            var redirectUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? "/Index" : ReturnUrl;
            return Redirect(redirectUrl);
        }
    }
}
