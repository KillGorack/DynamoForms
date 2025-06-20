using System.Collections.Generic;
using System.Threading.Tasks;
using DynamoForms.Data;

namespace DynamoForms.Data
{
    public class AppSettings
    {
        private readonly DatabaseHelper _dbHelper;

        public AppSettings(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<Dictionary<string, object>> LoadAsDictionaryAsync(string tableName)
        {
            var sql = "SELECT * FROM Application WHERE Var = @Var";
            var parameters = new { Var = tableName };
            var result = await _dbHelper.FetchDataAsync(sql, 1, parameters);

            return result as Dictionary<string, object> ?? new Dictionary<string, object>();
        }
    }
}