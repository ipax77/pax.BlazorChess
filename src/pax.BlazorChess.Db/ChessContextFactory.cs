using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace pax.BlazorChess.Db;

public class ChessContextFactory : IDesignTimeDbContextFactory<ChessContext>
{
    public ChessContext CreateDbContext(string[] args)
    {
        var sqliteConnectionString = $"Data Source={Path.Combine("/data/chess", "blazorChess.db")}";

        var optionsBuilder = new DbContextOptionsBuilder<ChessContext>();
        optionsBuilder.UseSqlite(sqliteConnectionString, options =>
        {
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            options.CommandTimeout(300);
            options.MigrationsAssembly("pax.BlazorChess.Db");
        });

        return new ChessContext(optionsBuilder.Options);
    }
}
