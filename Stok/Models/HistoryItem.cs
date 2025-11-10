using System.Collections.Generic;
using System.Text.Json;
using SQLite;

namespace Stok.Models
{
    public enum HistoryType
    {
        StockIn,
        StockOut,
        Delete,
        Case,
        Checklist
    }

    [Table("HistoryItems")]
    public class HistoryItem
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        public HistoryType Type { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string DetailsJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedBy { get; set; } = string.Empty;

        public bool Reversible { get; set; }

        public Guid? ReferenceId { get; set; }

        [Ignore]
        public IList<UsedMaterialRecord> Details
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DetailsJson))
                {
                    return new List<UsedMaterialRecord>();
                }

                try
                {
                    var records = JsonSerializer.Deserialize<List<UsedMaterialRecord>>(DetailsJson);
                    return records ?? new List<UsedMaterialRecord>();
                }
                catch
                {
                    return new List<UsedMaterialRecord>();
                }
            }
            set
            {
                DetailsJson = JsonSerializer.Serialize(value ?? new List<UsedMaterialRecord>());
            }
        }
    }
}
