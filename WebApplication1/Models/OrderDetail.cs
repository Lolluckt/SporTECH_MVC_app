using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }
        [Required]
        public int TrainerId { get; set; }
        public Trainer Trainer { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Кількість повинна бути більше 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна повинна бути більше 0")]
        public decimal Price { get; set; }
    }
}