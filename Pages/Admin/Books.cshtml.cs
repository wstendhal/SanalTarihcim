using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SanalTarihcim.Models;
using SanalTarihcim.Services;

namespace SanalTarihcim.Pages.Admin;

public class BooksModel(BookCatalogService bookCatalog) : PageModel
{
    [BindProperty]
    public Guid? Id { get; set; }

    [BindProperty, Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(120)]
    public string Author { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [BindProperty, Required, StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [BindProperty, StringLength(300)]
    public string? DetailUrl { get; set; }

    public IReadOnlyList<Book> Books { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public IActionResult OnGet(Guid? editId)
    {
        if (!IsAdmin())
        {
            return RedirectToPage("/Admin/Login");
        }

        Books = bookCatalog.GetAll();
        var book = editId.HasValue ? Books.FirstOrDefault(item => item.Id == editId.Value) : null;
        if (book is not null)
        {
            Id = book.Id;
            Title = book.Title;
            Author = book.Author;
            Description = book.Description;
            ImageUrl = book.ImageUrl;
            DetailUrl = book.DetailUrl;
        }

        return Page();
    }

    public IActionResult OnPostSave()
    {
        if (!IsAdmin())
        {
            return RedirectToPage("/Admin/Login");
        }

        if (!ModelState.IsValid)
        {
            Books = bookCatalog.GetAll();
            return Page();
        }

        var book = new Book(Id ?? Guid.NewGuid(), Title.Trim(), Author.Trim(), Description.Trim(), ImageUrl.Trim(), NormalizeOptional(DetailUrl));
        if (Id.HasValue)
        {
            if (!bookCatalog.Update(book))
            {
                return NotFound();
            }
        }
        else
        {
            bookCatalog.Add(book);
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(Guid id)
    {
        if (!IsAdmin())
        {
            return RedirectToPage("/Admin/Login");
        }

        bookCatalog.Delete(id);
        return RedirectToPage();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("IsAdmin");
        HttpContext.Session.Remove("AdminEmail");
        return RedirectToPage("/Admin/Login");
    }

    private bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "true";

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}