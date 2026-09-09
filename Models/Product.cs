using System;
using System.Collections.Generic;

namespace Project.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Brand { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? Stock { get; set; }

    public int? CategoryId { get; set; }

    public string? ImageUrl { get; set; } 
    
    public int UsId { get; set; }         

    public virtual Category? Category { get; set; }

    public int? PromotionId { get; set; }
}