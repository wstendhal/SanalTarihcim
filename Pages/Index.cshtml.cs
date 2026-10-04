using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Models;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages;

public class IndexModel : PageModel
{
    private readonly BookCatalogService _bookCatalog;

    public IndexModel(BookCatalogService bookCatalog)
    {
        _bookCatalog = bookCatalog;
    }

    public IReadOnlyList<Book> Books { get; private set; } = [];

    public void OnGet() => Books = _bookCatalog.GetAll();
}
