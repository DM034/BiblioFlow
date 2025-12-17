using Microsoft.AspNetCore.Mvc;
using Biblio.FrontOffice.Data;

namespace Biblio.FrontOffice.Controllers.Api;

[ApiController]
[Route("api")]
public class LoansApiController : ControllerBase
{
    private readonly SqlLibraryRepository _repo;
    public LoansApiController(SqlLibraryRepository repo) => _repo = repo;

    private string UserEmail => Request.Headers["X-User-Email"].ToString();

    [HttpPost("books/{bookId:int}/borrow")]
    public async Task<IActionResult> Borrow(int bookId)
    {
        if (string.IsNullOrWhiteSpace(UserEmail)) return BadRequest("Missing X-User-Email");

        var loanId = await _repo.BorrowAsync(bookId, UserEmail, days: 14);
        if (loanId == null) return Conflict("No seat available");
        return Ok(new { loanId });
    }

    [HttpPost("loans/{loanId:int}/return")]
    public async Task<IActionResult> Return(int loanId)
    {
        if (string.IsNullOrWhiteSpace(UserEmail)) return BadRequest("Missing X-User-Email");

        await _repo.ReturnAsync(loanId, UserEmail);
        return Ok();
    }

    [HttpGet("books/{bookId:int}/read")]
    public async Task<IActionResult> Read(int bookId)
    {
        if (string.IsNullOrWhiteSpace(UserEmail)) return BadRequest("Missing X-User-Email");

        var ok = await _repo.HasActiveLoanAsync(bookId, UserEmail);
        if (!ok) return Forbid();

        var path = await _repo.GetPdfPathAsync(bookId);
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            return NotFound("PDF not found");

        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/pdf", enableRangeProcessing: true);
    }
}
