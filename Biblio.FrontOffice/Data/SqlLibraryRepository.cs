using Microsoft.Data.SqlClient;

namespace Biblio.FrontOffice.Data;

public class SqlLibraryRepository
{
    private readonly string _cs;
    public SqlLibraryRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("Default")!;

    public async Task<(List<BookRow> items, int total)> GetBooksAsync(int page, int pageSize, string? q, bool onlyAvailable = false)
    {
        var items = new List<BookRow>();
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        // WHERE search
        var whereSearch = string.IsNullOrWhiteSpace(q) ? "" : "AND (b.Title LIKE @q OR b.Author LIKE @q)";

        // Count with availability filter
        var sqlCount = $@"
SELECT COUNT(*)
FROM Books b
OUTER APPLY (
   SELECT TOP 1 ConcurrentSeats AS Seats
   FROM Licenses l
   WHERE l.BookId = b.Id
) lic
OUTER APPLY (
   SELECT COUNT(*) AS ActiveLoans
   FROM Loans lo
   WHERE lo.BookId = b.Id AND lo.ReturnedAt IS NULL AND GETUTCDATE() <= lo.DueAt
) act
WHERE 1=1
{whereSearch}
AND (@onlyAvail = 0 OR act.ActiveLoans < ISNULL(lic.Seats, 1));
";
        await using var cmdCount = new SqlCommand(sqlCount, cn);
        cmdCount.Parameters.AddWithValue("@onlyAvail", onlyAvailable ? 1 : 0);
        if (!string.IsNullOrWhiteSpace(q)) cmdCount.Parameters.AddWithValue("@q", $"%{q}%");
        var total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());

        // Data page
        var sql = $@"
SELECT
  b.Id,
  b.Title,
  b.Author,
  b.Category,
  b.Year,
  b.Summary,
  ISNULL(lic.Seats, 1) AS Seats,
  act.ActiveLoans AS ActiveLoans
FROM Books b
OUTER APPLY (
   SELECT TOP 1 ConcurrentSeats AS Seats
   FROM Licenses l
   WHERE l.BookId = b.Id
) lic
OUTER APPLY (
   SELECT COUNT(*) AS ActiveLoans
   FROM Loans lo
   WHERE lo.BookId = b.Id AND lo.ReturnedAt IS NULL AND GETUTCDATE() <= lo.DueAt
) act
WHERE 1=1
{whereSearch}
AND (@onlyAvail = 0 OR act.ActiveLoans < ISNULL(lic.Seats, 1))
ORDER BY b.Title
OFFSET @off ROWS FETCH NEXT @ps ROWS ONLY;
";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@onlyAvail", onlyAvailable ? 1 : 0);
        if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
        cmd.Parameters.AddWithValue("@off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@ps", pageSize);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var id = r.GetInt32(0);
            var title = r.IsDBNull(1) ? "" : r.GetString(1);
            var author = r.IsDBNull(2) ? "" : r.GetString(2);
            var category = r.IsDBNull(3) ? "General" : r.GetString(3);
            int? year = r.IsDBNull(4) ? null : r.GetInt32(4);
            var summary = r.IsDBNull(5) ? "" : r.GetString(5);

            var seats = r.IsDBNull(6) ? 1 : r.GetInt32(6);
            var active = r.IsDBNull(7) ? 0 : r.GetInt32(7);
            var available = active < seats;

            items.Add(new BookRow(id, title, author, category, year, summary, seats, active, available));
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

        var seatsCmd = new SqlCommand("SELECT TOP 1 ConcurrentSeats FROM Licenses WHERE BookId=@b", cn);
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

    public async Task ReturnByBookAsync(int bookId, string userEmail)
    {
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var cmd = new SqlCommand(@"
UPDATE Loans SET ReturnedAt = GETUTCDATE()
WHERE BookId=@b AND UserEmail=@u AND ReturnedAt IS NULL;", cn);

        cmd.Parameters.AddWithValue("@b", bookId);
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

    public async Task<List<UserLoanRow>> GetUserLoansAsync(string userEmail)
    {
        var items = new List<UserLoanRow>();
        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        var sql = @"
SELECT l.Id, l.BookId, b.Title, l.StartAt, l.DueAt, l.ReturnedAt
FROM Loans l
JOIN Books b ON b.Id = l.BookId
WHERE l.UserEmail=@u
ORDER BY l.ReturnedAt ASC, l.DueAt ASC;";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@u", userEmail);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            items.Add(new UserLoanRow(
                r.GetInt32(0),
                r.GetInt32(1),
                r.IsDBNull(2) ? "" : r.GetString(2),
                r.GetDateTime(3),
                r.GetDateTime(4),
                r.IsDBNull(5) ? null : r.GetDateTime(5)
            ));
        }

        return items;
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
}

public record BookRow(
    int Id, string Title, string Author, string Category, int? Year, string Summary,
    int Seats, int ActiveLoans, bool Available
);
public record UserLoanRow(int LoanId, int BookId, string Title, DateTime StartAt, DateTime DueAt, DateTime? ReturnedAt);

