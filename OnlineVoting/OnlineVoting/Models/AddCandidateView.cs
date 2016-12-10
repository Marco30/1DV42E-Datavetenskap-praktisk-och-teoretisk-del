using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OnlineVoting.Models
{
    public class AddCandidateView
    {
        public int VotingId { get; set; }
        //Model för drop down list som användas för att läga till medlemar i valet 
        [Required(ErrorMessage = "You must select an user...")]
        public int UserId { get; set; }
    }
}