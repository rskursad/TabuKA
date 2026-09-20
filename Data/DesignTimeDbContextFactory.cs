using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TabuKA.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TabuKADbContext>
{
    public TabuKADbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TabuKADbContext>();
        optionsBuilder.UseSqlite("Data Source=tabuka.db");
        return new TabuKADbContext(optionsBuilder.Options);
    }
}