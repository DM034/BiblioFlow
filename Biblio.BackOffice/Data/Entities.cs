using System.ComponentModel.DataAnnotations;

namespace Biblio.BackOffice.Data;

public class Book
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = "";

    [Required]
    public string Author { get; set; } = "";

    public string Category { get; set; } = "General";

    public int? Year { get; set; }

    public string? Summary { get; set; }

    // nullable => pas obligé d’uploader au moment de créer
    public string? PdfPath { get; set; }

    public License? License { get; set; }
    public List<Loan> Loans { get; set; } = new();
}

public class License
{
    // PK = FK vers Book
    [Key]
    public int BookId { get; set; }
    public int ConcurrentSeats { get; set; } = 1;

    public Book? Book { get; set; }
}

public class Loan
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Required]
    public string UserEmail { get; set; } = "";

    public DateTime StartAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
