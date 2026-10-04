using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages.Admin
{
    public class IndexModel(BookCatalogService bookCatalog) : PageModel
    {
        public string AdminEmail { get; private set; } = "Admin";
        public int BookCount { get; private set; }

        public IActionResult OnGet()
        {
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (string.IsNullOrWhiteSpace(isAdmin) || isAdmin != "true")
            {
                return Redirect("/Admin/Login");
            }

            AdminEmail = HttpContext.Session.GetString("AdminEmail") ?? "admin@sanal-tarihcim.com";
            BookCount = bookCatalog.GetAll().Count;
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Remove("IsAdmin");
            HttpContext.Session.Remove("AdminEmail");
            return RedirectToPage("/Admin/Login");
        }
    }
}
