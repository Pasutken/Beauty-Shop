using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("promotions")]
public class Promotion
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = null!;
    public string DiscountType { get; set; } = null!;
    public int DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}