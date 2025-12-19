namespace Biblio.BackOffice.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public int? Year { get; set; }
    public string? Summary { get; set; }

    // Modèle A: le PDF reste sur le serveur
    public string? PdfPath { get; set; }

    public License? License { get; set; }
}
