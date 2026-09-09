using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("order_details")] // <== 1. แปะป้ายโยงไปตาราง order_details
public class OrderDetails
{
    [Key] // <== 2. แปะป้ายชี้เป้า Primary Key ให้ระบบเลิกงง!
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public decimal Price { get; set; }
    
    public int Quantity { get; set; }
}