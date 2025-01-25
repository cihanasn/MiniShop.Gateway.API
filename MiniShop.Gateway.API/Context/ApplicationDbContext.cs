using Microsoft.EntityFrameworkCore;

namespace MiniShop.Gateway.API.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Örnek bir tablo
    public DbSet<MiniShop.Gateway.API.Models.User> Users { get; set; }
}
