using SQLite;

namespace Stok.Models
{
    [Table("Users")]
    public class User
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Indexed(Unique = true)]
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
