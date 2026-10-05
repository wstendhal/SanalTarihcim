using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SanalTarihcim.Pages
{
    public class SepetOnayModel : PageModel
    {
        public IActionResult OnGet()
        {
            var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            if (isLoggedIn != "true")
            {
                return Redirect("/Giris?returnUrl=/SepetOnay");
            }

            return Page();
        }
    }
}
