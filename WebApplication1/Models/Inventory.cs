using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Models;

namespace WebApplication1.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        [ForeignKey("Trainer")]
        public int TrainerId { get; set; }
        public int Quantity { get; set; }
        public Trainer Trainer { get; set; }
    }
}