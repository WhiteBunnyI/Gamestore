using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    [ForeignKey("CountryId")]
    [InverseProperty("Publishers")]
    public Country Country { get; set; } = null!;

    [JsonIgnore]
    [InverseProperty("Publisher")]
    public ICollection<Game> Games { get; set; } = new List<Game>();
}
