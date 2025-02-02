using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Trainer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Display(Name = "Опис")]
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal ProductionCost { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public Inventory? Inventory { get; set; }
        public int Quantity { get; set; }
    }
}