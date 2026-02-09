using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class GameParticipant
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("GameSession")]
        public int GameSessionId { get; set; }
        public GameSession GameSession { get; set; }

        [Required]
        [ForeignKey("Player")]
        public int PlayerId { get; set; }
        public Player Player { get; set; }

        [Required]
        public int FinalScore { get; set; } = 0;
        [Required]
        public int FinalRank { get; set; } = 0;
    }
}