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

        [BindProperty(SupportsGet = true)]
        public string? EmailQuery { get; set; }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Kod { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Message { get; set; }

        public string? InfoMessage { get; private set; }

        public void OnGet()
        {
            Email = (EmailQuery ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(Message))
            {
                InfoMessage = Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Kod))
            {
                InfoMessage = "E-posta ve doğrulama kodu zorunludur.";
                return Page();
            }

            var isValid = await _authService.VerifyRegistrationCodeAsync(Email, Kod);
            if (!isValid)
            {
                InfoMessage = "Doğrulama kodu geçersiz veya süresi dolmuş.";
                return Page();
            }

            var redirectUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/SepetOnay";
            return RedirectToPage("/Giris", new { EmailQuery = Email, ReturnUrl = redirectUrl });
        }
    }
}
