using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Keyless]
[Table("game_user", Schema = "gamestore")]
[Index("GameId", "UserId", Name = "game_user_game_id_user_id_key", IsUnique = true)]
public class GameUser
{
    [Column("game_id")]
    public int GameId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("date_purchase")]
    public DateOnly DatePurchase { get; set; }

    [Column("price")]
    public float Price { get; set; }

    [JsonIgnore]
    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;

    [JsonIgnore]
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
