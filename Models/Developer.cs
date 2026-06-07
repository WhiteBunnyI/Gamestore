using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Gamestore.Models;

[Table("developer", Schema = "gamestore")]
[Index("Name", Name = "developer_name_key", IsUnique = true)]
public class Developer
{
    public class DeveloperDto
    {
        [Required]
        [StringLength(30)]
        public string DeveloperName { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string CountryName { get; set; } = null!;
    }

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
    [InverseProperty("Developers")]
    public Country Country { get; set; } = null!;
}
