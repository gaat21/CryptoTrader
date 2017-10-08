using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradingTester.DAL.Models;

namespace TradingTester.DAL
{
    public interface ITradingDbContext
    {
        DbSet<CandleDto> Candles { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
