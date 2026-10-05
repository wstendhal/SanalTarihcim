using System.Text.Json;
using SanalTarihcim.Models;

namespace SanalTarihcim.Services;

public sealed class BookCatalogService
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private List<Book> _books;

    public BookCatalogService(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "books.json");

        if (File.Exists(_filePath))
        {
            _books = JsonSerializer.Deserialize<List<Book>>(File.ReadAllText(_filePath)) ?? [];
        }
        else
        {
            _books = CreateInitialBooks();
            Save();
        }
    }

    public IReadOnlyList<Book> GetAll()
    {
        lock (_sync)
        {
            return _books.ToList();
        }
    }

    public void Add(Book book)
    {
        lock (_sync)
        {
            _books.Add(book);
            Save();
        }
    }

    public bool Update(Book book)
    {
        lock (_sync)
        {
            var index = _books.FindIndex(existing => existing.Id == book.Id);
            if (index < 0)
            {
                return false;
            }

            _books[index] = book;
            Save();
            return true;
        }
    }

    public bool Delete(Guid id)
    {
        lock (_sync)
        {
            var removed = _books.RemoveAll(book => book.Id == id) > 0;
            if (removed)
            {
                Save();
            }

            return removed;
        }
    }

    private void Save()
    {
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_books, _jsonOptions));
    }

    private static List<Book> CreateInitialBooks() =>
    [
        new(
            Guid.NewGuid(),
            "Osmanlı Tarihine Kuşbakışı",
            "Halil İnalcık",
            "İmparatorluğun yükseliş ve çöküş dinamiklerini anlatan bir başyapıt.",
            "/osmkitap.jfif",
            "/Osmanli"),
        new(
            Guid.NewGuid(),
            "Roma İmparatorluğu Tarihi",
            "Edward Gibbon",
            "Antik dünyanın en büyük gücünün yükselişini ve mirasını keşfedin.",
            "/romakitap.jfif",
            "/Roma"),
        new(
            Guid.NewGuid(),
            "Savaş Ve Barış",
            "Lev Tolstoy",
            "Napolyon savaşlarının gölgesinde Rus toplumunun ruhunu yansıtan tarih panoraması.",
            "/savaskitap.jfif",
            "/SavasTarihi"),
        new(
            Guid.NewGuid(),
            "Sapiens",
            "Yuval Noah Harari",
            "İnsanoğlunun avcı-toplayıcılıktan günümüze uzanan yolculuğu.",
            "https://images.unsplash.com/photo-1589829085413-56de8ae18c73?q=80&w=1000",
            "/ModernTarih"),
        new(
            Guid.NewGuid(),
            "Tüfek, Mikrop ve Çelik",
            "Jared Diamond",
            "Coğrafyanın ve ekolojinin toplumların kaderi üzerindeki etkisi.",
            "/tufek.jfif",
            "/DunyaTarihi"),
        new(
            Guid.NewGuid(),
            "Mukaddime",
            "İbn-i Haldun",
            "Devletlerin doğuşunu, yükselişini ve çöküşünü anlatan tarih felsefesi eseri.",
            "/mukaddime.jfif",
            "/IslamTarihi"),
        new(
            Guid.NewGuid(),
            "Sefiller",
            "Victor Hugo",
            "Fransız Devrimi yıllarında adalet ve vicdan arayışının hikayesi.",
            "/sefiller.jfif",
            "/AvrupaTarihi"),
        new(
            Guid.NewGuid(),
            "Nutuk",
            "Mustafa Kemal Atatürk",
            "Milli Mücadele'nin safhalarını liderinin anlatımıyla aktaran arşiv belgesi.",
            "/nutuk.jfif",
            "/TurkTarihciler")
    ];
}