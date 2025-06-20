using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamoForms.Models;

namespace DynamoForms.Data
{
    public class FieldDefinitions
    {
        private readonly DatabaseHelper _dbHelper;
        private readonly Dictionary<string, List<TableColumnMeta>> _metaCache = new();

        public FieldDefinitions(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /// <summary>
        /// Loads and caches meta data for a table, and returns as DynamicFormField dictionary.
        /// </summary>
        public async Task<Dictionary<string, DynamicFormField>> LoadAsync(string tableName)
        {
            if (!_metaCache.TryGetValue(tableName, out var meta))
            {
                meta = await _dbHelper.GetTableMetaAsync(tableName);
                _metaCache[tableName] = meta;
            }

            var fields = meta.ToDictionary(
                m => m.ColumnName,
                m => new DynamicFormField
                {
                    Name = m.ColumnName,
                    Type = m.DataType,
                    Length = m.MaxLength?.ToString(),
                    Precision = null,
                    IsUnique = m.IsPrimaryKey,
                    Required = !m.IsNullable,
                    IsIdentity = m.IsIdentity
                }
            );

            return fields;
        }

        public List<TableColumnMeta> ToColumnMeta(Dictionary<string, DynamicFormField> fields)
        {
            if (fields == null) return new List<TableColumnMeta>();
            return fields.Values.Select(f => new TableColumnMeta
            {
                ColumnName = f.Name,
                DataType = f.Type,
                IsNullable = !f.Required,
                IsIdentity = f.IsIdentity,
                IsPrimaryKey = f.IsUnique, // Adjust if needed
                MaxLength = int.TryParse(f.Length, out var len) ? len : null
            }).ToList();
        }

        /// <summary>
        /// Gets the cached TableColumnMeta list for a table (after LoadAsync has been called).
        /// </summary>
        public List<TableColumnMeta> GetMeta(string tableName)
        {
            return _metaCache.TryGetValue(tableName, out var meta) ? meta : null;
        }
    }
}