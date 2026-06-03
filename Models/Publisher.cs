using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

[Table("publisher", Schema = "gamestore")]
[Index("Name", Name = "publisher_name_key", IsUnique = true)]
public class Publisher
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(30)]
    public string Name { get; set; } = null!;

    [Column("country_id")]
    public int CountryId { get; set; }

    [ForeignKey("CountryId")]
    [InverseProperty("Publishers")]
    public Country Country { get; set; } = null!;

    [InverseProperty("Publisher")]
    public ICollection<Game> Games { get; set; } = new List<Game>();
}
