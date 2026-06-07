using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.Models;

[Table("user", Schema = "gamestore")]
[Index("Login", Name = "user_login_key", IsUnique = true)]
public class User
{
    public class UserDto
    {
        [Required]
        [StringLength(30)]
        public string Login { get; set; } = null!;
    }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("login")]
    [StringLength(30)]
    public string Login { get; set; } = null!;

    [Column("wallet")]
    public float Wallet { get; set; }
}
