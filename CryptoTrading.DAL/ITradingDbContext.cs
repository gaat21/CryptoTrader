using System.Threading.Tasks;
using CryptoTrading.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.DAL
{
    public interface ITradingDbContext
    {
        DbSet<CandleDto> Candles { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
