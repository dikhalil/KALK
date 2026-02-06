using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Role { get; set; } = "Player"; 

        [MaxLength(100)]
        public string? AvatarImageName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       [Required]
        public Player? Player { get; set; } = null!;
    }
}
