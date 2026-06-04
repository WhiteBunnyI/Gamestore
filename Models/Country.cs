using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Table("country", Schema = "gamestore")]
[Index("Name", Name = "country_name_key", IsUnique = true)]
public class Country
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(30)]
    public string Name { get; set; } = null!;

    [JsonIgnore]
    [InverseProperty("Country")]
    public ICollection<Publisher> Publishers { get; set; } = new List<Publisher>();
}
