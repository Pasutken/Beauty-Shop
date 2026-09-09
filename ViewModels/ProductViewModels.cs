using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Project.ViewModels
{
    public class ProductViewModels
    {
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "กรุณากรอกชื่อสินค้า")]
        public string ProductName { get; set; } = null!;
        
        public string? Brand { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "กรุณากรอกราคา")]
        public decimal Price { get; set; }
        
        public int? Stock { get; set; }
        
        public int? CategoryId { get; set; }
        
        // 1. ImageUrl: เอาไว้เก็บ "ชื่อไฟล์รูปภาพ" ที่ดึงมาจาก Database เพื่อเอาไปโชว์ในหน้าเว็บ
        public string? ImageUrl { get; set; } 
        
        // 2. ImageFile: เอาไว้รับ "ก้อนไฟล์รูปภาพจริงๆ" ที่ผู้ใช้กดอัปโหลด (Browse) เข้ามาจากหน้าเว็บ
        public IFormFile? ImageFile { get; set; } 
    }
}