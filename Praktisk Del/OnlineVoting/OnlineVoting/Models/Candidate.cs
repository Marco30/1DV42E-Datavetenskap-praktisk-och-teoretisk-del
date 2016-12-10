using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OnlineVoting.Models
{
    public class Candidate
    {
        //används vid röstnings för att visa användare i valet och så man kan rösta
        [Key]
        public int CandidateId { get; set; }

        public int VotingId { get; set; }

        public int UserId { get; set; }

        public int QuantityVotes { get; set; }

        public virtual Voting Voting { get; set; }
        public virtual User User { get; set; }

        public virtual ICollection<VotingDetail> VotingDetails { get; set; }
    }
}