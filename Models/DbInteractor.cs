using Microsoft.EntityFrameworkCore;
using StripeApi.Models;

namespace SubscribeApi.Models
{
    public class DbInteractor : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
            .UseSqlite(@"Data Source = SubscriberApiDb.db;");
        }

        public DbSet<Subscriber> Subscribers { get; set; }
    }
}
