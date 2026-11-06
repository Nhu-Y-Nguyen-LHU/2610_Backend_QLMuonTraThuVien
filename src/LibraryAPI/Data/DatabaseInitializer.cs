using LibraryAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext db, IHostEnvironment environment)
    {
        db.Database.Migrate();

        var scriptPath = Path.Combine(environment.ContentRootPath, "..", "..", "database", "sp_BorrowBook.sql");
        if (!File.Exists(scriptPath))
            scriptPath = Path.Combine(AppContext.BaseDirectory, "database", "sp_BorrowBook.sql");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("The sp_BorrowBook database script was not found.", scriptPath);

        var script = string.Join(
            Environment.NewLine,
            File.ReadAllLines(scriptPath).Where(line => !line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase)));
        db.Database.ExecuteSqlRaw(script);
    }
}
