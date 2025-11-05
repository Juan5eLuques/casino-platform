using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Casino.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Para migraciones, usar una conexión por defecto
        optionsBuilder.UseNpgsql("Host=shortline.proxy.rlwy.net;Port=47433;Database=railway;Username=postgres;Password=dzPvAkviRrmLjpinAeNakUymDpWaHVuq");
        
        return new AppDbContext(optionsBuilder.Options);
    }
}