using DynamoForms.Data;
using DynamoForms.Models;

namespace DynamoForms.Data
{
    public class AppRegistry
    {
        public Dictionary<string, object> ValidatedQuery { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public Dictionary<string, UnifiedField> Fields { get; set; }
        public List<TableColumnMeta> Columns { get; set; } // <-- Add this
    }
}