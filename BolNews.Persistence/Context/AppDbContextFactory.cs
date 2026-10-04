using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BolNews.Persistence.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        
        public AppDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .Build();
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            //Connection string for MySQL database local
            var connectionString = "server=192.168.75.129; port=3306; database=BolNewsDB; user=admin_user; password =Bol12345";
            //Get the connection string from Azure
            //var connectionString = "server=bolnewsdb.mysql.database.azure.com; port=3306; database=bolnewsdb; user=admin_user; password =Bol12345";
            optionsBuilder.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(5, 6, 4)));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
