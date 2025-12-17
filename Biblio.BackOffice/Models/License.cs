namespace Biblio.BackOffice.Models;

public class License
{
    public int BookId { get; set; }
    public int ConcurrentSeats { get; set; } = 1;

    public Book Book { get; set; } = null!;
}
