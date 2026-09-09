using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Models;

[Table("user_promotions")]
public class UserPromotion
{
    [Key]
    public int UserPromoId { get; set; }
    public int UsId { get; set; }
    public int PromotionId { get; set; }
    public bool IsUsed { get; set; }
}