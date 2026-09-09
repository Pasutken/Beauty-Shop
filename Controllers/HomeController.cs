using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using Project.ViewModels;
using System.Text.Json;

namespace Project.Controllers;

public class HomeController : Controller
{

    private readonly ProjectContext _db;

    public HomeController(ProjectContext db)
    {
        _db = db;
    }

    public IActionResult Home()
    {
        var products = _db.Products.OrderByDescending(p => p.ProductId).Take(4).ToList();
        return View(products);
    }

    public IActionResult Contact()
    {
        return View();
    }

    // หน้า Dashboard 

    // หน้าแสดงสรุปข้อมูลและกราฟต่างๆ สำหรับเจ้าของร้าน
    [HttpGet]
    public IActionResult Dashboard()
    {
        // สรุปข้อมูลการ์ด 4 ใบด้านบน
        ViewBag.TotalRevenue = _db.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.TotalOrders = _db.Orders.Count();
        ViewBag.LowStockCount = _db.Products.Count(p => p.Stock < 5);
        ViewBag.ActivePromos = _db.Promotions.Count(p => p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);

        // ดึงรายการสั่งซื้อล่าสุด 5 รายการ เพื่อเอาไปโชว์ในตาราง
        var recentOrders = _db.Orders
                              .OrderByDescending(o => o.OrderDate)
                              .Take(5)
                              .ToList();

        // เตรียมข้อมูลกราฟยอดขาย 7 วันย้อนหลัง 
        var startDate = DateTime.Today.AddDays(-6);

        // ดึงออเดอร์เฉพาะช่วง 7 วันที่ผ่านมา
        var last7DaysOrders = _db.Orders
                                 .Where(o => o.OrderDate >= startDate)
                                 .ToList();

        var labels = new List<string>();
        var salesData = new List<decimal>();

        // วนลูปสร้างข้อมูลทีละวัน (7 วัน)
        for (int i = 0; i < 7; i++)
        {
            var currentDate = startDate.AddDays(i);
            labels.Add(currentDate.ToString("dd MMM")); 

            // รวมยอดขายของวันนั้นๆ
            var dailyTotal = last7DaysOrders
                                .Where(o => o.OrderDate?.Date == currentDate.Date)
                                .Sum(o => o.TotalAmount);

            salesData.Add(dailyTotal);
        }

        // แปลงข้อมูลเป็น JSON เพื่อส่งไปให้ JavaScript วาดกราฟ
        ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        ViewBag.ChartData = System.Text.Json.JsonSerializer.Serialize(salesData);

        return View(recentOrders);
    }

    // ระบบจัดการออเดอร์ (สำหรับเจ้าของร้าน)

    //  หน้าแสดงออเดอร์ทั้งหมดให้ร้านค้าดู
    [HttpGet]
    public IActionResult ManageOrders()
    {

        var allOrders = _db.Orders
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();
        return View(allOrders);
    }

    //  ร้านค้ากดปุ่ม "จัดส่งสินค้าแล้ว"
    [HttpPost]
    public IActionResult ShipOrder(int orderId)
    {
        var order = _db.Orders.Find(orderId);
        if (order != null && order.Status == "Pending")
        {
            order.Status = "Shipped"; // เปลี่ยนสถานะเป็นจัดส่งแล้ว
            _db.SaveChanges();
            TempData["AdminMsg"] = $"อัปเดตสถานะออเดอร์ #{orderId} เป็นจัดส่งแล้ว!";
        }
        return RedirectToAction("ManageOrders");
    }

    // ระบบจัดการออเดอร์ (สำหรับลูกค้า)

    // ลูกค้ากดยืนยันว่า "ได้รับสินค้าแล้ว"
    [HttpPost]
    public IActionResult ReceiveOrder(int orderId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // ค้นหาออเดอร์ที่ตรงกับ orderId และเป็นของลูกค้าคนนี้ และสถานะต้องเป็น "Shipped" เท่านั้น
        var order = _db.Orders.FirstOrDefault(o => o.OrderId == orderId && o.UsId == userId.Value);
        if (order != null && order.Status == "Shipped")
        {
            order.Status = "Completed"; // เปลี่ยนสถานะเป็นเสร็จสมบูรณ์
            _db.SaveChanges();
            TempData["SuccessMsg"] = $"ยืนยันการรับสินค้า ออเดอร์ #{orderId} สำเร็จ ขอบคุณที่อุดหนุนครับ!";
        }
        return RedirectToAction("Orders");
    }

    // ระบบตะกร้าสินค้า (Shopping Cart)

    // ฟังก์ชันช่วย: อ่านข้อมูลตะกร้าจาก Session
    private List<CartItemViewModel> GetCart()
    {
        var cartJson = HttpContext.Session.GetString("Cart");
        if (string.IsNullOrEmpty(cartJson))
            return new List<CartItemViewModel>();
        return JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
    }

    // ฟังก์ชันช่วย: เซฟข้อมูลตะกร้ากลับลง Session
    private void SaveCart(List<CartItemViewModel> cart)
    {
        var cartJson = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString("Cart", cartJson);
    }

    // หน้าแสดงตะกร้าสินค้า 
    public IActionResult Cart()
    {
        var cart = GetCart();
        return View(cart);
    }

    // เพิ่มสินค้าลงตะกร้า
    public IActionResult AddToCart(int id, int qty = 1)
    {
        // หาสินค้าจากฐานข้อมูลด้วย id ที่ส่งมา
        var product = _db.Products.Find(id);
        if (product == null) return RedirectToAction("Product", "Product");

        // ตรวจสอบว่ามีสินค้านี้ในตะกร้าหรือไม่
        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(c => c.ProductId == id);

        if (existingItem != null)
        {
            // ถ้ามีสินค้านี้ในตะกร้าแล้ว ให้บวกจำนวนเพิ่มเข้าไปตามที่ลูกค้าระบุ
            existingItem.Quantity += qty;
        }
        else
        {
            // ถ้ายังไม่มี ให้เพิ่มเป็นรายการใหม่ พร้อมจำนวน
            cart.Add(new CartItemViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = qty
            });
        }

        SaveCart(cart);
        // แจ้งเตือนลูกค้าหน่อยว่าเพิ่มสำเร็จแล้ว
        TempData["CartSuccess"] = $"เพิ่ม {product.ProductName} ลงตะกร้าแล้ว!";

        // อ่าน URL ของหน้าเดิมที่ลูกค้าเพิ่งกดปุ่มมา
        string referer = Request.Headers["Referer"].ToString();

        // ถ้ามีหน้าเดิม ให้เด้งกลับไปหน้าเดิม (หน้าจะรีเฟรช 1 ครั้ง แต่อยู่ที่เดิม)
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }

        return RedirectToAction("Product", "Product");
    }

    // อัปเดตจำนวนสินค้าในตะกร้า
    public IActionResult UpdateCart(int id, int qty)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == id);

        if (item != null)
        {
            if (qty > 0)
            {
                // ถ้าจำนวนมากกว่า 0 ให้อัปเดตเป็นจำนวนใหม่
                item.Quantity = qty;
            }
            else
            {
                // ถ้ากดลดจนเหลือ 0 ให้ลบสินค้านั้นออกจากตะกร้าไปเลย
                cart.Remove(item);
            }
            SaveCart(cart);
        }

        return RedirectToAction("Cart");
    }

    // ลบสินค้าออกจากตะกร้า
    public IActionResult RemoveFromCart(int id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == id);

        if (item != null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }

        return RedirectToAction("Cart");
    }

    // ล้างตะกร้าทั้งหมด
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("Cart");
        return RedirectToAction("Cart"); 
    }

    // ระบบสั่งซื้อสินค้า 
    
    // Checkout ดึงเฉพาะคูปองที่ "เก็บแล้วและยังไม่ใช้"
    public IActionResult Checkout()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = _db.Users.FirstOrDefault(u => u.UsId == userId.Value);
        if (user != null)
        {
            ViewBag.UserPhone = user.UsPhone;       
            ViewBag.UserAddress = user.UsAddress;   
        }

        var cart = GetCart();
        if (!cart.Any()) return RedirectToAction("Cart");

        // ดึงรหัสโปรโมชั่นจากกระเป๋าคูปองของ User
        var myPromoIds = _db.UserPromotions
                            .Where(up => up.UsId == userId && !up.IsUsed)
                            .Select(up => up.PromotionId).ToList();

        // ดึงรายละเอียดโปรโมชั่นเหล่านั้น
        ViewBag.MyPromotions = _db.Promotions
                                  .Where(p => myPromoIds.Contains(p.PromotionId) && p.EndDate >= DateTime.Now)
                                  .ToList();

        // ข้อมูลส่วนลดที่เลือกใช้ (ถ้ามี)
        ViewBag.SelPromoId = HttpContext.Session.GetInt32("PromoId");
        ViewBag.DiscountType = HttpContext.Session.GetString("DiscountType");
        ViewBag.DiscountValue = HttpContext.Session.GetString("DiscountValue");

        return View(cart);
    }

    // บันทึกการสั่งซื้อ 
    [HttpPost]
    public IActionResult ConfirmOrder(string shippingAddress, string phone)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var cart = GetCart();
        if (!cart.Any()) return RedirectToAction("Cart");

        decimal subTotal = cart.Sum(c => c.Total);
        decimal shippingFee = 50; // ค่าส่งเริ่มต้น 50 บาท
        decimal discountAmt = 0;

        // คำนวณส่วนลดจาก Session 
        string distType = HttpContext.Session.GetString("DiscountType") ?? "";
        decimal distVal = 0;
        decimal.TryParse(HttpContext.Session.GetString("DiscountValue"), out distVal);

        if (distType == "Percent") discountAmt = subTotal * (distVal / 100.0m);
        else if (distType == "Amount") discountAmt = distVal;
        else if (distType == "FreeShipping") shippingFee = 0; // ส่งฟรี!

        if (discountAmt > subTotal) discountAmt = subTotal;
        decimal finalTotal = subTotal - discountAmt + shippingFee;

        // บันทึก Order ลงฐานข้อมูลหลัก
        var order = new Orders
        {
            UsId = userId.Value,
            SellerId = 1, 
            OrderDate = DateTime.Now,
            ShippingAddress = shippingAddress,
            Phone = phone,
            TotalAmount = finalTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmt,
            Status = "Pending"
        };
        _db.Orders.Add(order);
        _db.SaveChanges(); 

        // บันทึกรายการสินค้าลง OrderDetails และ ตัดสต็อก 
        foreach (var item in cart)
        {
            var detail = new OrderDetails
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity
            };
            _db.OrderDetails.Add(detail);

            // ค้นหาสินค้าเพื่อตัดสต็อก
            var product = _db.Products.Find(item.ProductId);
            if (product != null)
            {
                product.Stock -= item.Quantity;
                if (product.Stock < 0) product.Stock = 0;
            }
        }

        // มาร์คคูปองว่า "ใช้แล้ว"
        int? selPromoId = HttpContext.Session.GetInt32("PromoId");
        if (selPromoId != null)
        {
            var up = _db.UserPromotions.FirstOrDefault(x => x.UsId == userId && x.PromotionId == selPromoId && !x.IsUsed);
            if (up != null) up.IsUsed = true;
        }

        _db.SaveChanges(); 

        // ล้างตะกร้าและล้างคูปอง
        HttpContext.Session.Remove("Cart");
        HttpContext.Session.Remove("PromoId");
        HttpContext.Session.Remove("DiscountType");
        HttpContext.Session.Remove("DiscountValue");
        HttpContext.Session.Remove("PromoTitle");

        return RedirectToAction("CheckoutSuccess", new { id = order.OrderId });
    }

    // เวลากดปุ่ม "ใช้คูปอง" ในหน้าชำระเงิน
    [HttpPost]
    public IActionResult ApplyPromo(int promotionId)
    {
        // ถ้าลูกค้าเลือก "-- ไม่ใช้คูปองส่วนลด --" (value = 0) ให้ล้างค่าทิ้ง
        if (promotionId == 0)
        {
            HttpContext.Session.Remove("PromoId");
            HttpContext.Session.Remove("DiscountType");
            HttpContext.Session.Remove("DiscountValue");
            HttpContext.Session.Remove("PromoTitle");
            return RedirectToAction("Checkout");
        }

        // หาคูปองจากฐานข้อมูล
        var promo = _db.Promotions.FirstOrDefault(p => p.PromotionId == promotionId);

        if (promo != null)
        {
            // ถ้าเจอคูปอง ให้จำค่าส่วนลดไว้ใน Session เพื่อเอาไปคำนวณหน้าเว็บ
            HttpContext.Session.SetInt32("PromoId", promo.PromotionId);
            HttpContext.Session.SetString("DiscountType", promo.DiscountType);
            HttpContext.Session.SetString("DiscountValue", promo.DiscountValue.ToString());
            HttpContext.Session.SetString("PromoTitle", promo.Title);

            TempData["PromoSuccess"] = "ใช้งานคูปองสำเร็จ!";
        }
        else
        {
            TempData["PromoError"] = "เกิดข้อผิดพลาด คูปองนี้ไม่สามารถใช้งานได้";
            HttpContext.Session.Remove("PromoId");
        }

        return RedirectToAction("Checkout");
    }

    // คูปองของฉัน
    [HttpGet]
    public IActionResult MyCoupons()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // ดึงรหัสคูปองที่ลูกค้าคนนี้ "เก็บแล้ว" และ "ยังไม่ได้ใช้"
        var myPromoIds = _db.UserPromotions
                            .Where(up => up.UsId == userId.Value && !up.IsUsed)
                            .Select(up => up.PromotionId)
                            .ToList();

        // ดึงรายละเอียดของคูปองเหล่านั้น ที่ "ยังไม่หมดอายุ" มาแสดง
        var myCoupons = _db.Promotions
                           .Where(p => myPromoIds.Contains(p.PromotionId) && p.EndDate >= DateTime.Now)
                           .ToList();

        return View(myCoupons);
    }

    // ประวัติการสั่งซื้อ 
    [HttpGet]
    public IActionResult Orders()
    {
        // เช็คว่าล็อกอินหรือยัง
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // ดึงออเดอร์ของลูกค้าคนนี้ เรียงจากล่าสุดไปเก่าสุด
        var myOrders = _db.Orders
                          .Where(o => o.UsId == userId.Value)
                          .OrderByDescending(o => o.OrderDate)
                          .ToList();

        // ดึงรายละเอียดสินค้าในแต่ละออเดอร์
        var orderIds = myOrders.Select(o => o.OrderId).ToList();

        // ไปค้นในตาราง OrderDetails และผูกกับตาราง Products เพื่อเอารูปและชื่อสินค้า
        var orderDetails = (from od in _db.OrderDetails
                            join p in _db.Products on od.ProductId equals p.ProductId
                            where orderIds.Contains(od.OrderId)
                            select new { od.OrderId, p.ProductName, p.ImageUrl, od.Quantity, od.Price }).ToList();

        // จัดกลุ่มสินค้าให้ตรงกับแต่ละบิล แล้วแพ็คใส่กระเป๋า ViewBag ไปให้หน้าเว็บ
        var dictItems = new Dictionary<int, List<Project.ViewModels.CartItemViewModel>>();
        foreach (var id in orderIds)
        {
            dictItems[id] = orderDetails.Where(x => x.OrderId == id)
                                        .Select(x => new Project.ViewModels.CartItemViewModel
                                        {
                                            ProductName = x.ProductName,
                                            ImageUrl = x.ImageUrl,
                                            Quantity = x.Quantity,
                                            Price = x.Price
                                        }).ToList();
        }
        ViewBag.OrderItems = dictItems;

        return View(myOrders);
    }

    // หน้าสั่งซื้อสำเร็จ
    public IActionResult CheckoutSuccess(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }

    // ระบบเก็บคูปอง
    [HttpGet]
    public IActionResult Promotion()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        // ดึงโปรโมชั่นทั้งหมดที่ยังไม่หมดอายุมาโชว์
        var activePromos = _db.Promotions
                              .Where(p => p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                              .ToList();

        // ดึงข้อมูลว่า User คนนี้ "เก็บคูปอง" อะไรไปแล้วบ้าง จะได้โชว์ปุ่ม "เก็บแล้ว"
        ViewBag.CollectedPromoIds = new List<int>();
        if (userId != null)
        {
            ViewBag.CollectedPromoIds = _db.UserPromotions
                                           .Where(up => up.UsId == userId.Value)
                                           .Select(up => up.PromotionId)
                                           .ToList();
        }

        return View(activePromos);
    }

    // เก็บคูปอง
    [HttpPost]
    public IActionResult CollectPromo(int id)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // เช็คว่าเคยเก็บไปหรือยัง
        var exist = _db.UserPromotions.Any(up => up.UsId == userId && up.PromotionId == id);
        if (!exist)
        {
            _db.UserPromotions.Add(new UserPromotion { UsId = userId.Value, PromotionId = id, IsUsed = false });
            _db.SaveChanges();
            TempData["Success"] = "เก็บคูปองสำเร็จ!";
        }
        return RedirectToAction("Promotion");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
