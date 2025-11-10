using SQLite;

namespace Stok.Models
{
    public enum LookupType
    {
        Hospital,
        Doctor
    }

    [Table("LookupValues")]
    public class LookupValue
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Indexed]
        public LookupType Type { get; set; }

        [Indexed]
        public string Value { get; set; } = string.Empty;
    }
}
