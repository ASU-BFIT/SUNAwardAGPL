using System.Data.Entity;

namespace SUNAward.Data
{
    public class SUNAwardContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Award> Awards { get; set; }

        static SUNAwardContext()
        {
            // don't attempt to validate database or perform migrations
            Database.SetInitializer<SUNAwardContext>(null);
        }
    }
}