using System.ComponentModel.DataAnnotations; 

namespace Backend.Models
{
    public class Player
    {
        [Key] public int Id { get; set; }
        [Required] [MaxLength(50)] public string Username { get; set; } = string.Empty;
        [MaxLength(100)] public string? AvatarImageName { get; set; }
        [MaxLength(50)] public string? OAuthProvider { get; set; }
        [MaxLength(100)] public string? OAuthExternalId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}