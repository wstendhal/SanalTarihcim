using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages
{
    public class UyeOlModel : PageModel
    {
        private readonly AuthService _authService;

        public UyeOlModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Ad { get; set; } = string.Empty;

        [BindProperty]
        public string Soyad { get; set; } = string.Empty;

        [BindProperty]
        public string Eposta { get; set; } = string.Empty;

        [BindProperty]
        public string Parola { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? InfoMessage { get; private set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Ad) || string.IsNullOrWhiteSpace(Soyad) ||
                string.IsNullOrWhiteSpace(Eposta) || string.IsNullOrWhiteSpace(Parola))
            {
                InfoMessage = "Lütfen tüm alanları doldurun.";
                return Page();
            }

            var result = await _authService.RegisterAsync(Ad, Soyad, Eposta, Parola);

            if (!result.Success)
            {
                InfoMessage = result.Message;
                return Page();
            }

            var redirectUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? "/SepetOnay" : ReturnUrl;
            var code = result.VerificationCode ?? string.Empty;
            var message = Uri.EscapeDataString(result.Message);
            return Redirect($"/Dogrulama?email={Uri.EscapeDataString(Eposta)}&returnUrl={Uri.EscapeDataString(redirectUrl)}&message={message}&code={Uri.EscapeDataString(code)}");
        }
    }
}
