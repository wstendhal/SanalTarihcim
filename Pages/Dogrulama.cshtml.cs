using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages
{
    public class DogrulamaModel : PageModel
    {
        private readonly AuthService _authService;

        public DogrulamaModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Kod { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Message { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Code { get; set; }

        public string? InfoMessage { get; private set; }

        public void OnGet()
        {
            if (!string.IsNullOrWhiteSpace(Email))
            {
                Email = Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(Code))
            {
                Kod = Code;
            }

            if (!string.IsNullOrWhiteSpace(Message))
            {
                InfoMessage = Message;
            }
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Kod))
            {
                InfoMessage = "E-posta ve doğrulama kodu zorunludur.";
                return Page();
            }

            var isValid = _authService.VerifyRegistrationCode(Email, Kod);
            if (!isValid)
            {
                InfoMessage = "Doğrulama kodu geçersiz veya süresi dolmuş.";
                return Page();
            }

            return Redirect(string.IsNullOrWhiteSpace(ReturnUrl) ? "/Giris?email=" + Uri.EscapeDataString(Email) : ReturnUrl);
        }
    }
}
