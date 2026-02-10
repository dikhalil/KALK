using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs
{
    public class PlayerLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}