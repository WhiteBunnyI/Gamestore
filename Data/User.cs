using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gamestore.Data
{
    [Table("user", Schema = "gamestore")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("login")]
        public string Login { get; set; } = string.Empty;

        [Column("wallet")]
        [Range(0, float.MaxValue, ErrorMessage = "Баланс не может быть отрицательным")]
        public float Wallet { get; set; } = 0f;
    }
}
