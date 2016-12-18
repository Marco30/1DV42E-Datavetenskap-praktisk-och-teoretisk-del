using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVoting.Models
{
    [NotMapped]
    public class VotingIndexView : Voting
    {
        // vinare 
        public User Winner { get; set; }
    }
}
