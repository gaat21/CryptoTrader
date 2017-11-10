using System.Threading.Tasks;
using CryptoTrading.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.DAL
{    
    public class TradingDbContext : DbContext, ITradingDbContext
    {
        public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
        {            

        }

        public DbSet<CandleDto> Candles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CandleDto>().ToTable("Candles");
        }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }
    }
}

