using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;
using Stok.Models;

namespace Stok.Services
{
    public class DatabaseService
    {
        private const string DatabaseFileName = "stok.db3";
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private SQLiteAsyncConnection? _connection;

        public async Task InitializeAsync()
        {
            if (_connection != null)
            {
                return;
            }

            await _initializationSemaphore.WaitAsync();
            try
            {
                if (_connection != null)
                {
                    return;
                }

                var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
                _connection = new SQLiteAsyncConnection(databasePath);
                await _connection.CreateTableAsync<Material>();
                await _connection.CreateTableAsync<CaseRecord>();
                await _connection.CreateTableAsync<HistoryItem>();
                await _connection.CreateTableAsync<User>();
                await _connection.CreateTableAsync<ChecklistItem>();
                await _connection.CreateTableAsync<LookupValue>();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await InitializeAsync();
            return _connection ?? throw new InvalidOperationException("Database connection could not be established.");
        }

        public async Task<IList<Material>> GetMaterialsAsync()
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<Material>().OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<Material?> GetMaterialAsync(Guid id)
        {
            var connection = await GetConnectionAsync();
            return await connection.FindAsync<Material>(id);
        }

        public async Task<int> SaveMaterialAsync(Material material)
        {
            material.UpdatedAt = DateTime.UtcNow;
            var connection = await GetConnectionAsync();
            var existing = await connection.FindAsync<Material>(material.Id);
            if (existing == null)
            {
                material.CreatedAt = DateTime.UtcNow;
                return await connection.InsertAsync(material);
            }

            return await connection.UpdateAsync(material);
        }

        public async Task<int> DeleteMaterialAsync(Material material)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(material);
        }

        public async Task<IList<CaseRecord>> GetCaseRecordsAsync()
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<CaseRecord>().OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<CaseRecord?> GetCaseRecordAsync(Guid id)
        {
            var connection = await GetConnectionAsync();
            return await connection.FindAsync<CaseRecord>(id);
        }

        public async Task<int> SaveCaseRecordAsync(CaseRecord record)
        {
            var connection = await GetConnectionAsync();
            var existing = await connection.FindAsync<CaseRecord>(record.Id);
            if (existing == null)
            {
                return await connection.InsertAsync(record);
            }

            return await connection.UpdateAsync(record);
        }

        public async Task<int> DeleteCaseRecordAsync(CaseRecord record)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(record);
        }

        public async Task<IList<HistoryItem>> GetHistoryAsync()
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<HistoryItem>().OrderByDescending(h => h.CreatedAt).ToListAsync();
        }

        public async Task<int> SaveHistoryItemAsync(HistoryItem item)
        {
            var connection = await GetConnectionAsync();
            var existing = await connection.FindAsync<HistoryItem>(item.Id);
            if (existing == null)
            {
                return await connection.InsertAsync(item);
            }

            return await connection.UpdateAsync(item);
        }

        public async Task<int> DeleteHistoryItemAsync(HistoryItem item)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(item);
        }

        public async Task<IList<User>> GetUsersAsync()
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<User>().OrderBy(u => u.Username).ToListAsync();
        }

        public async Task<User?> FindUserByUsernameAsync(string username)
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<User>().Where(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserAsync(Guid id)
        {
            var connection = await GetConnectionAsync();
            return await connection.FindAsync<User>(id);
        }

        public async Task<int> SaveUserAsync(User user)
        {
            var connection = await GetConnectionAsync();
            var existing = await connection.FindAsync<User>(user.Id);
            if (existing == null)
            {
                return await connection.InsertAsync(user);
            }

            return await connection.UpdateAsync(user);
        }

        public async Task<int> DeleteUserAsync(User user)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(user);
        }

        public async Task<IList<ChecklistItem>> GetChecklistItemsAsync()
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<ChecklistItem>().OrderBy(item => item.OrderNo).ToListAsync();
        }

        public async Task<int> SaveChecklistItemAsync(ChecklistItem item)
        {
            var connection = await GetConnectionAsync();
            var existing = await connection.FindAsync<ChecklistItem>(item.Id);
            if (existing == null)
            {
                return await connection.InsertAsync(item);
            }

            return await connection.UpdateAsync(item);
        }

        public async Task<int> DeleteChecklistItemAsync(ChecklistItem item)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(item);
        }

        public async Task<IList<LookupValue>> GetLookupValuesAsync(LookupType type)
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<LookupValue>()
                .Where(entry => entry.Type == type)
                .OrderBy(entry => entry.Value)
                .ToListAsync();
        }

        public async Task<LookupValue?> FindLookupValueAsync(LookupType type, string value)
        {
            var connection = await GetConnectionAsync();
            return await connection.Table<LookupValue>()
                .Where(entry => entry.Type == type && entry.Value == value)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveLookupValueAsync(LookupValue entry)
        {
            var connection = await GetConnectionAsync();
            var existing = await FindLookupValueAsync(entry.Type, entry.Value);
            if (existing == null)
            {
                return await connection.InsertAsync(entry);
            }

            entry.Id = existing.Id;
            return await connection.UpdateAsync(entry);
        }

        public async Task<int> DeleteLookupValueAsync(LookupValue entry)
        {
            var connection = await GetConnectionAsync();
            return await connection.DeleteAsync(entry);
        }
    }
}
