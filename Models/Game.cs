using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

[Table("game", Schema = "gamestore")]
[Index("Title", Name = "game_title_key", IsUnique = true)]
public class Game
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(50)]
    public string Title { get; set; } = null!;

    [Column("description")]
    [StringLength(2000)]
    public string Description { get; set; } = null!;

    [Column("date_release")]
    public DateOnly DateRelease { get; set; }

    [Column("price")]
    public float Price { get; set; }

    [Column("system_required")]
    [StringLength(500)]
    public string SystemRequired { get; set; } = null!;

    [Column("publisher_id")]
    public int PublisherId { get; set; }

    [InverseProperty("Game")]
    public ICollection<GameVersion> GameVersions { get; set; } = new List<GameVersion>();

    [ForeignKey("PublisherId")]
    [InverseProperty("Games")]
    public Publisher Publisher { get; set; } = null!;
}
