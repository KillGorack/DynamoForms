using System.Threading.Tasks;
using DynamoForms.Data;
using Microsoft.AspNetCore.Http;

namespace DynamoForms.Services
{
    public class AppRegistryService
    {
        private readonly DatabaseHelper _dbHelper;

        public AppRegistryService(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<AppRegistry> BuildAsync(string appVar, IQueryCollection query)
        {
            var registry = new AppRegistry();
            var validator = new QueryStringValidator(_dbHelper);
            var appSettings = new AppSettings(_dbHelper);
            var fieldDefs = new FieldDefinitions(_dbHelper);

            registry.ValidatedQuery = await validator.ValidateAsync(query);
            registry.Settings = await appSettings.LoadAsDictionaryAsync(appVar);
            registry.Fields = await fieldDefs.LoadAsync(appVar);
            registry.Columns = fieldDefs.ToColumnMeta(registry.Fields);

            return registry;
        }
    }
}