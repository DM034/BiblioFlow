namespace Biblio.BackOffice.Models;

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string UserEmail { get; set; } = "";

    public DateTime StartAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public Book Book { get; set; } = null!;
}
