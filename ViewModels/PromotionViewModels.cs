using System;
using System.ComponentModel.DataAnnotations;

namespace Project.ViewModels
{
    public class PromotionViewModels
    {
        public int PromotionId { get; set; }
        [Required(ErrorMessage = "กรุณากรอกชื่อโปรโมชั่น")]
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        [Required(ErrorMessage = "กรุณาเลือกประเภทส่วนลด")]
        public string DiscountType { get; set; } = null!;
        public decimal? DiscountValue { get; set; }
        [Required(ErrorMessage = "กรุณาระบุวันเริ่มโปรโมชั่น")]
        public DateTime StartDate { get; set; }
        [Required(ErrorMessage = "กรุณาระบุวันสิ้นสุดโปรโมชั่น")]
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}