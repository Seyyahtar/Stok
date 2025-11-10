using System.Collections.Generic;
using SQLite;

namespace Stok.Models
{
    [Table("Materials")]
    public class Material
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Indexed]
        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Serial { get; set; }

        public string? Lot { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int Quantity { get; set; }

        public string? Ubb { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string OwnerUser { get; set; } = string.Empty;

        [Ignore]
        public string SerialOrLotDisplay
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Serial))
                {
                    parts.Add($"Seri: {Serial}");
                }

                if (!string.IsNullOrWhiteSpace(Lot))
                {
                    parts.Add($"Lot: {Lot}");
                }

                if (ExpiryDate.HasValue)
                {
                    parts.Add($"SKT: {ExpiryDate:dd.MM.yyyy}");
                }

                return parts.Count > 0 ? string.Join(" • ", parts) : "Detay bulunmuyor";
            }
        }
    }
}
