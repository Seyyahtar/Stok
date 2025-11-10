using System.Collections.Generic;
using System.Text.Json;
using SQLite;

namespace Stok.Models
{
    [Table("CaseRecords")]
    public class CaseRecord
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Indexed]
        public string Hospital { get; set; } = string.Empty;

        public string Doctor { get; set; } = string.Empty;

        public string Patient { get; set; } = string.Empty;

        public string? Note { get; set; }

        public string UsedMaterialsJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        [Ignore]
        public IList<UsedMaterialRecord> UsedMaterials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(UsedMaterialsJson))
                {
                    return new List<UsedMaterialRecord>();
                }

                try
                {
                    var records = JsonSerializer.Deserialize<List<UsedMaterialRecord>>(UsedMaterialsJson);
                    return records ?? new List<UsedMaterialRecord>();
                }
                catch
                {
                    return new List<UsedMaterialRecord>();
                }
            }
            set
            {
                UsedMaterialsJson = JsonSerializer.Serialize(value ?? new List<UsedMaterialRecord>());
            }
        }
    }
}
