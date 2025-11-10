namespace Stok.Models
{
    public class UsedMaterialRecord
    {
        public Guid? MaterialId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? SerialOrLot { get; set; }

        public string? Serial { get; set; }

        public string? Lot { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int Quantity { get; set; }
    }
}
