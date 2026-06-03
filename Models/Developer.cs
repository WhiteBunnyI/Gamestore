using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

[Table("developer", Schema = "gamestore")]
[Index("Name", Name = "developer_name_key", IsUnique = true)]
public class Developer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(30)]
    public string Name { get; set; } = null!;

    [Column("country_id")]
    public int CountryId { get; set; }
}
