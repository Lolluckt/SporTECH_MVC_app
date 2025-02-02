using System;
using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int ManagerId { get; set; }
        public int? ClientDetailsId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public User Manager { get; set; }
        public ClientDetails ClientDetails { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}