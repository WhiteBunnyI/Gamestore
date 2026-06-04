using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Keyless]
[Table("game_developer", Schema = "gamestore")]
[Index("GameId", "DeveloperId", Name = "game_developer_game_id_developer_id_key", IsUnique = true)]
public class GameDeveloper
{
    [Column("game_id")]
    public int GameId { get; set; }

    [Column("developer_id")]
    public int DeveloperId { get; set; }

    [JsonIgnore]
    [ForeignKey("DeveloperId")]
    public Developer Developer { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;
}
