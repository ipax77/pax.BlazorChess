using Microsoft.EntityFrameworkCore;

namespace pax.BlazorChess.Db;

public class ChessContext : DbContext
{
    public ChessContext(DbContextOptions<ChessContext> options) : base(options)
    {
    }
}
