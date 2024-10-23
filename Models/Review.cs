using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mailo.Models;

namespace Mailo.Models
{
    public class Review
    {
        
        [ForeignKey("user")]
        public int UserID { get; set; }
        public User user { get; set; }
        [ForeignKey("product")]
        public int ProductID { get; set; }
        public Product product { get; set; }
		[Range(1, 5)]
		public float Rating { get; set; }
		[MaxLength(50)]
		public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
   
    }
}
