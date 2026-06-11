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
    public class PublisherDto
    {
        [Required]
        [StringLength(30)]
        public string PublisherName { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string CountryName { get; set; } = null!;

        //Тк получаем через тело (FromBody, т.е. через json), то он не будет заполнен, его надо заполнить вручную
        //Кроме того оно будет игнорироваться и при остальных атрибутах (FromQuery и тд), т.к. не имеет св-в get и set
        //[JsonIgnore]
        //public int CountryId;
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
    [InverseProperty("Publishers")]
    public Country Country { get; set; } = null!;

    [JsonIgnore]
    [InverseProperty("Publisher")]
    public ICollection<Game> Games { get; set; } = new List<Game>();
}
