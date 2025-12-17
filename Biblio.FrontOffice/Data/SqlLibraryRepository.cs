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
}

public record BookRow(int Id, string Title, string Author, string Category, int? Year, string Summary);
