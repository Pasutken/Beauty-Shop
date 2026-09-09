using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("orders")] // <== 1. แปะป้ายบอกว่าให้โยงไปหาตารางชื่อ orders (ตัวเล็ก)
public class Orders
{
    [Key] // <== 2. แปะป้ายบอกว่านี่แหละคือ Primary Key!
    public int OrderId { get; set; }

    public int UsId { get; set; }

    public int SellerId { get; set; }

    public string? ShippingAddress { get; set; }

    public string? Phone { get; set; }
    
    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal ShippingFee { get; set; }
    
    public decimal DiscountAmount { get; set; }
    
    public string? Status { get; set; }
}