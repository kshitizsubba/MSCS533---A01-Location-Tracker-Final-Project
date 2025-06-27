using SQLite;
using LocationHeatMapApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocationHeatMapApp.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<UserLocation>().Wait();
        }

        public Task<int> InsertLocationAsync(UserLocation location)
        {
            return _database.InsertAsync(location);
        }

        public Task<List<UserLocation>> GetLocationsAsync()
        {
            return _database.Table<UserLocation>().ToListAsync();
        }
    }
}
