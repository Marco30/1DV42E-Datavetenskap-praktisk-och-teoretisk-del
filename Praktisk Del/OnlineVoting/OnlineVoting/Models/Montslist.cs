using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OnlineVoting.Models
{
    public class MontsList
    {

        //[Required(ErrorMessage = "The field {0} is required")]
        public int MonthsID { get; set; }

        //[Required(ErrorMessage = "The field {0} is required")]
        public String Months { get; set; }
    }
}