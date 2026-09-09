using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Project.Models;
using Project.ViewModels;

namespace Project.Controllers;

public class ProductController : Controller
{
    private readonly ProjectContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductController(ProjectContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ฟังก์ชันช่วยเช็คสิทธิ์ (ป้องกันคนพิมพ์ URL เข้ามาโดยไม่ได้ Login)
    private bool IsSeller()
    {
        var role = HttpContext.Session.GetString("RoleName")?.ToLower();
        return role == "seller" || role == "admin";
    }


    // หน้าแสดงสินค้าทั้งหมด
    [HttpGet]
    public IActionResult Product(int? categoryId, string search) 
    {
        // ดึงหมวดหมู่ทั้งหมดส่งไปทำปุ่ม Filter ที่หน้าเว็บ
        ViewBag.Categories = _db.Categories.ToList();

        // ส่ง ID และคำค้นหาปัจจุบันไปให้หน้าเว็บ เพื่อให้ปุ่ม Filter และช่องค้นหาโชว์ค่าเดิมค้างไว้
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SearchTerm = search;

        // ดึงสินค้าที่มีสต็อกรอไว้
        var query = _db.Products.Where(p => p.Stock > 0);

        // ถ้ามีการกดเลือกหมวดหมู่เข้ามา ให้กรองเอาเฉพาะหมวดหมู่นั้น
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // ถ้ามีการพิมพ์ค้นหาเข้ามา ให้กรองคำค้นหา 
        if (!string.IsNullOrEmpty(search))
        {
            // ค้นหาทั้งจาก ชื่อสินค้า และ ชื่อแบรนด์
            query = query.Where(p => p.ProductName.Contains(search) || p.Brand.Contains(search));
        }

        var products = query.Select(p => new ProductViewModels
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Brand = p.Brand,
            Price = p.Price,
            ImageUrl = p.ImageUrl
        }).ToList();

        return View(products);
    }

    // (Seller Only)

    /// หน้าร้านค้าของฉัน
    public IActionResult MyStore()
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");

        // ดึงสินค้าของ Seller คนนี้มาโชว์
        int? userId = HttpContext.Session.GetInt32("UserId");

        // ถ้ายังไม่ได้ Login ให้ไปหน้า Login
        if (userId == null) return RedirectToAction("Login", "Account");

        var products = _db.Products.Where(p => p.UsId == userId).Select(p => new ProductViewModels
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Brand = p.Brand,
            Price = p.Price,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl
        }).ToList();

        return View(products);
    }

    // หน้าเพิ่มสินค้า
    [HttpGet]
    public IActionResult AddProduct()
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");

        ViewBag.Categories = _db.Categories.ToList();
        return View();
    }

    // บันทึกสินค้าใหม่
    [HttpPost]
    public async Task<IActionResult> AddProduct(ProductViewModels data)
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");

        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        string uniqueFileName = null;

        if (data.ImageFile != null)
        {
            // สร้างโฟลเดอร์ uploads ใน wwwroot ถ้ายังไม่มี
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // สร้างชื่อไฟล์ที่ไม่ซ้ำกันเพื่อป้องกันการทับไฟล์
            uniqueFileName = Guid.NewGuid().ToString() + "_" + data.ImageFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // อัปโหลดไฟล์
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await data.ImageFile.CopyToAsync(fileStream);
            }
        }

        var newProduct = new Product
        {
            ProductName = data.ProductName,
            Brand = data.Brand,
            Description = data.Description,
            Price = data.Price,
            Stock = data.Stock,
            CategoryId = data.CategoryId,
            ImageUrl = uniqueFileName,
            UsId = userId.Value
        };

        _db.Products.Add(newProduct);
        await _db.SaveChangesAsync();

        return RedirectToAction("MyStore");
    }

    // หน้าแก้ไขสินค้า
    [HttpGet]
    public IActionResult EditProduct(int id)
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");
        int? userId = HttpContext.Session.GetInt32("UserId");

        // ดึงข้อมูลสินค้าเข้ามาแสดงในฟอร์ม โดยเช็คด้วยว่า สินค้านี้เป็นของ Seller คนนี้จริงๆ หรือเปล่า
        var product = _db.Products.FirstOrDefault(p => p.ProductId == id && p.UsId == userId);
        if (product == null) return RedirectToAction("MyStore");

        ViewBag.Categories = _db.Categories.ToList();

        var model = new ProductViewModels
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Brand = product.Brand,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl
        };

        return View(model);
    }

    // บันทึกการแก้ไขสินค้า
    [HttpPost]
    public async Task<IActionResult> EditProduct(ProductViewModels data)
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");
        int? userId = HttpContext.Session.GetInt32("UserId");
        var product = _db.Products.FirstOrDefault(p => p.ProductId == data.ProductId && p.UsId == userId);

        if (product != null)
        {
            product.ProductName = data.ProductName;
            product.Brand = data.Brand;
            product.Description = data.Description;
            product.Price = data.Price;
            product.Stock = data.Stock;
            product.CategoryId = data.CategoryId;

            if (data.ImageFile != null)
            {
                // ถ้ามีการอัปโหลดรูปใหม่เข้ามา ให้ลบรูปเก่าออกไปก่อน (ถ้ามี)
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                // อัปโหลดไฟล์ใหม่
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await data.ImageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = uniqueFileName;
            }

            await _db.SaveChangesAsync();
        }

        return RedirectToAction("MyStore");
    }

    // ลบสินค้า
    public IActionResult DeleteProduct(int id)
    {
        if (!IsSeller()) return RedirectToAction("Home", "Home");
        int? userId = HttpContext.Session.GetInt32("UserId");

        var product = _db.Products.FirstOrDefault(p => p.ProductId == id && p.UsId == userId);

        if (product != null)
        {
            _db.Products.Remove(product);
            _db.SaveChanges();
        }

        return RedirectToAction("MyStore");
    }

    // หน้าแสดงรายการหมวดหมู่ทั้งหมด
    public IActionResult CategoryList()
    {
        // เช็คสิทธิ์ว่าเป็น Admin หรือไม่ (ถ้าไม่เป็นให้เด้งกลับ)
        var role = HttpContext.Session.GetString("RoleName")?.ToLower();
        if (role != "admin") return RedirectToAction("Home", "Home");

        var categories = _db.Categories.ToList();
        return View(categories);
    }

    // บันทึกหมวดหมู่ใหม่
    [HttpPost]
    public IActionResult AddCategory(string categoryName)
    {
        if (!string.IsNullOrEmpty(categoryName))
        {
            var cat = new Category { CategoryName = categoryName };
            _db.Categories.Add(cat);
            _db.SaveChanges();
        }
        return RedirectToAction("CategoryList");
    }

    // ลบหมวดหมู่
    public IActionResult DeleteCategory(int id)
    {
        var cat = _db.Categories.Find(id);
        if (cat != null)
        {
            _db.Categories.Remove(cat);
            _db.SaveChanges();
        }
        return RedirectToAction("CategoryList");
    }

    // หน้าแสดงรายละเอียดสินค้า (สำหรับลูกค้า)
    [HttpGet]
    public IActionResult Details(int id)
    {
        var product = _db.Products
                         .Where(p => p.ProductId == id)
                         .Select(p => new ProductViewModels
                         {
                             ProductId = p.ProductId,
                             ProductName = p.ProductName,
                             Brand = p.Brand,
                             Description = p.Description,
                             Price = p.Price,
                             Stock = p.Stock,
                             ImageUrl = p.ImageUrl,
                             CategoryId = p.CategoryId
                         }).FirstOrDefault();

        if (product == null) return RedirectToAction("Product");

        return View(product);
    }

    // ระบบโปรโมชั่น (สำหรับ Admin)

    // ตารางโชว์โปรโมชั่น
    public IActionResult PromotionList()
    {
        if (HttpContext.Session.GetString("RoleName")?.ToLower() != "admin")
            return RedirectToAction("Home", "Home");

        var promos = _db.Promotions.ToList();
        return View(promos);
    }

    // เพิ่มโปรโมชั่น
    [HttpGet]
    public IActionResult AddPromotion()
    {
        if (HttpContext.Session.GetString("RoleName")?.ToLower() != "admin")
            return RedirectToAction("Home", "Home");

        return View();
    }

    // บันทึกโปรโมชั่นใหม่
    [HttpPost]
    public IActionResult AddPromotion(Promotion data)
    {
        _db.Promotions.Add(data);
        _db.SaveChanges();

        return RedirectToAction("PromotionList");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}