using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Table("game_version", Schema = "gamestore")]
public class GameVersion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("game_id")]
    public int GameId { get; set; }

    [Column("date_release")]
    public DateOnly DateRelease { get; set; }

    [Column("description")]
    public string Description { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("GameId")]
    [InverseProperty("GameVersions")]
    public Game Game { get; set; } = null!;
}
