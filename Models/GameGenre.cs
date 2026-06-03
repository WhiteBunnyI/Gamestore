using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

[Keyless]
[Table("game_genre", Schema = "gamestore")]
[Index("GameId", "GenreId", Name = "game_genre_game_id_genre_id_key", IsUnique = true)]
public class GameGenre
{
    [Column("game_id")]
    public int GameId { get; set; }

    [Column("genre_id")]
    public int GenreId { get; set; }

    [ForeignKey("GameId")]
    public Game Game { get; set; } = null!;

    [ForeignKey("GenreId")]
    public Genre Genre { get; set; } = null!;
}
