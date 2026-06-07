using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Table("game", Schema = "gamestore")]
[Index("Title", Name = "game_title_key", IsUnique = true)]
public class Game
{
    public class GameDto
    {
        [Required]
        [Range(0f, float.MaxValue)]
        public float Price { get; set; }

        [Required] 
        public int PublisherId { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string SystemRequired { get; set; } = null!;

        public ICollection<int> Developers { get; set; } = new List<int>();
        public ICollection<int> Genres { get; set; } = new List<int>();
    }


    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(50)]
    [Required]
    public string Title { get; set; } = null!;

    [Column("description")]
    [StringLength(2000)]
    [Required]
    public string Description { get; set; } = null!;

    [Column("date_release")]
    [Required]
    public DateOnly DateRelease { get; set; }

    [Column("price")]
    [Required]
    public float Price { get; set; }

    [Column("system_required")]
    [StringLength(500)]
    [Required]
    public string SystemRequired { get; set; } = null!;

    [Column("publisher_id")]
    [Required]
    public int PublisherId { get; set; }

    [JsonIgnore]
    [InverseProperty("Game")]
    public ICollection<GameVersion> GameVersions { get; set; } = new List<GameVersion>();

    [JsonIgnore]
    [ForeignKey("PublisherId")]
    [InverseProperty("Games")]
    public Publisher Publisher { get; set; } = null!;
}
