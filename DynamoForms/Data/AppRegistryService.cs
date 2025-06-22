using DynamoForms.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace DynamoForms.Services
{
    public class AppRegistryService
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly AppRegistry _registry;

        public AppRegistryService(DatabaseHelper dbHelper, AppRegistry registry)
        {
            _dbHelper = dbHelper;
            _registry = registry;
        }

        public async Task<AppRegistry> BuildAsync(string appVar, IQueryCollection query)
        {
            // Create the AppRegistry object first
            var registry = new AppRegistry();

            // Query Strings
            var validator = new QueryStringValidator(_dbHelper);
            registry.ValidatedQuery = await validator.ValidateAsync(query);

            // Application settings
            var appSettings = new AppSettings(_dbHelper);
            registry.Settings = await appSettings.LoadAsDictionaryAsync(appVar);

            // Fields (Please work on getting rid of the columns)
            var fieldDefs = new FieldDefinitions(_dbHelper, registry);
            registry.Fields = await fieldDefs.LoadAsync(appVar);
            registry.Columns = fieldDefs.ToColumnMeta(registry.Fields);

            return registry;
        }
    }
}