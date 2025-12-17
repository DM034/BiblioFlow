using Microsoft.Data.SqlClient;

namespace Biblio.FrontOffice.Data;

public class SqlLibraryRepository
{
    private readonly string _cs;
    public SqlLibraryRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("Default")!;

    public async Task<(List<BookRow> items, int total)> GetBooksAsync(int page, int pageSize, string? q)
    {
        var items = new List<BookRow>();
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var where = string.IsNullOrWhiteSpace(q) ? "" : "WHERE Title LIKE @q OR Author LIKE @q";

        await using var cmdCount = new SqlCommand($"SELECT COUNT(*) FROM Books {where};", cn);
        if (!string.IsNullOrWhiteSpace(q)) cmdCount.Parameters.AddWithValue("@q", $"%{q}%");
        var total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());

        var sql = $@"
SELECT Id, Title, Author, Category, Year, Summary
FROM Books
{where}
ORDER BY Title
OFFSET @off ROWS FETCH NEXT @ps ROWS ONLY;";

        await using var cmd = new SqlCommand(sql, cn);
        if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
        cmd.Parameters.AddWithValue("@off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@ps", pageSize);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            items.Add(new BookRow(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetInt32(4),
                r.GetString(5)
            ));
        }

        return (items, total);
    }

    public async Task<bool> HasActiveLoanAsync(int bookId, string userEmail)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var sql = @"
SELECT TOP 1 1
FROM Loans
WHERE BookId=@b AND UserEmail=@u AND ReturnedAt IS NULL AND GETUTCDATE() <= DueAt;";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@b", bookId);
        cmd.Parameters.AddWithValue("@u", userEmail);
        return await cmd.ExecuteScalarAsync() != null;
    }

    public async Task<int?> BorrowAsync(int bookId, string userEmail, int days = 14)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        // seats (si pas de ligne Licenses, on considère 1)
        var seatsCmd = new SqlCommand("SELECT ConcurrentSeats FROM Licenses WHERE BookId=@b", cn);
        seatsCmd.Parameters.AddWithValue("@b", bookId);
        var seatsObj = await seatsCmd.ExecuteScalarAsync();
        var seats = seatsObj == null ? 1 : Convert.ToInt32(seatsObj);

        var cntCmd = new SqlCommand(@"
SELECT COUNT(*) FROM Loans
WHERE BookId=@b AND ReturnedAt IS NULL AND GETUTCDATE() <= DueAt;", cn);
        cntCmd.Parameters.AddWithValue("@b", bookId);
        var active = Convert.ToInt32(await cntCmd.ExecuteScalarAsync());

        if (active >= seats) return null;

        var ins = new SqlCommand(@"
INSERT INTO Loans(BookId, UserEmail, StartAt, DueAt, ReturnedAt)
OUTPUT INSERTED.Id
VALUES(@b, @u, GETUTCDATE(), DATEADD(day, @d, GETUTCDATE()), NULL);", cn);
        ins.Parameters.AddWithValue("@b", bookId);
        ins.Parameters.AddWithValue("@u", userEmail);
        ins.Parameters.AddWithValue("@d", days);

        return Convert.ToInt32(await ins.ExecuteScalarAsync());
    }

    public async Task ReturnAsync(int loanId, string userEmail)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var cmd = new SqlCommand(@"
UPDATE Loans SET ReturnedAt = GETUTCDATE()
WHERE Id=@id AND UserEmail=@u AND ReturnedAt IS NULL;", cn);
        cmd.Parameters.AddWithValue("@id", loanId);
        cmd.Parameters.AddWithValue("@u", userEmail);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string?> GetPdfPathAsync(int bookId)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var cmd = new SqlCommand("SELECT PdfPath FROM Books WHERE Id=@id;", cn);
        cmd.Parameters.AddWithValue("@id", bookId);
        return (string?)await cmd.ExecuteScalarAsync();
    }
}

public record BookRow(int Id, string Title, string Author, string Category, int? Year, string Summary);
