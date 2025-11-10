using SQLite;

namespace Stok.Models
{
    public enum ChecklistStatus
    {
        NotYet,
        Done
    }

    [Table("ChecklistItems")]
    public class ChecklistItem
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        public int OrderNo { get; set; }

        public string Patient { get; set; } = string.Empty;

        public string Hospital { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public TimeSpan Time { get; set; }

        public ChecklistStatus Status { get; set; } = ChecklistStatus.NotYet;
    }
}
