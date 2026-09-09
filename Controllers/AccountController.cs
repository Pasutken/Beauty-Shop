using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using Project.ViewModels;

namespace Project.Controllers;

public class AccountController : Controller
{

    private readonly ProjectContext _db;

    public AccountController(ProjectContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // ถ้ามี Session อยู่แล้ว ให้เด้งไปหน้า Home เลย
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
        {
            return RedirectToAction("Home", "Home");
        }
        return View();
    }

    [HttpPost]
    public IActionResult Login(string phone, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.UsPhone == phone && u.UsPassword == password);

        if (user == null)
        {
            ViewBag.Error = "เบอร์โทรศัพท์หรือรหัสผ่านไม่ถูกต้อง";
            return View();
        }

        // ดึงชื่อ Role เก็บใน Session
        var role = _db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
        string roleName = role != null ? role.RoleName.ToLower() : "Customer"; 

        // เก็บข้อมูลผู้ใช้ใน Session
        string userName = user.UsName ?? "สมาชิก";
        HttpContext.Session.SetString("UserName", userName);
        HttpContext.Session.SetInt32("UserId", user.UsId);

        // เก็บข้อมูล RoleName ใน Session
        HttpContext.Session.SetString("RoleName", roleName);

        if (roleName == "Admin")
        {
            return RedirectToAction("UserList", "Account");
        }

        else if (roleName == "Seller")
        {
            return RedirectToAction("MyStore", "Product");
        }

        else
        {
            return RedirectToAction("Home", "Home");
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Home", "Home");
    }

    
    public IActionResult Register()
    {
        return View();
    }

    // ลงทะเบียนผู้ใช้ใหม่
    [HttpPost]
    public IActionResult Register(RegisterUserViewModels data)
    {
        // เช็คเบอร์โทรศัพท์ซ้ำในฐานข้อมูล
        var existingUser = _db.Users.FirstOrDefault(u => u.UsPhone == data.Phone);
        if (existingUser != null)
        {
            ViewBag.Error = "เบอร์โทรศัพท์นี้ถูกลงทะเบียนไปแล้ว กรุณาใช้เบอร์อื่น หรือเข้าสู่ระบบ";
            return View(data);
        }

        if (data.Password.Length < 6)
        {
            ViewBag.Error = "รหัสผ่านต้องอย่างน้อย 6 ตัว";
            return View(data);
        }

        if (!data.Password.Any(char.IsUpper))
        {
            ViewBag.Error = "ต้องมีตัวพิมพ์ใหญ่ (A-Z)";
            return View(data);
        }

        if (!data.Password.Any(char.IsDigit))
        {
            ViewBag.Error = "ต้องมีตัวเลข";
            return View(data);
        }

        if (data.Password != data.PasswordAgain)
        {
            ViewBag.Error = "รหัสผ่านไม่ตรงกัน";
            return View(data);
        }

        var u = new User();

        u.UsName = data.Name;
        u.UsLastname = data.Lastname;
        u.UsPhone = data.Phone;
        u.UsPassword = data.Password;
        u.RoleId = 2;
        u.CreatedAt = DateTime.Now;

        _db.Users.Add(u);
        _db.SaveChanges();

        return RedirectToAction("Login", "Account");
    }

    // (Admin Only)

    // แสดงรายชื่อผู้ใช้ทั้งหมด 
    public IActionResult UserList()
    {
        // ถ้า RoleName ไม่ใช่ "admin" ให้เด้งกลับไปหน้า Home
        var role = HttpContext.Session.GetString("RoleName");
        if (role != "admin") return RedirectToAction("Home", "Home");

        var allRoles = _db.Roles.Select(r => new Project.ViewModels.RoleViewModels
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName
        }).ToList();

        var allUsers = _db.Users.ToList();

        var user = allUsers.Select(u => new RegisterUserViewModels
        {
            UsId = u.UsId,
            Name = u.UsName,
            Lastname = u.UsLastname,
            Phone = u.UsPhone,
            RoleId = u.RoleId,
            RoleName = allRoles.FirstOrDefault(r => r.RoleId == u.RoleId)?.RoleName
        }).ToList();

        return View(user);
    }

    // ลบผู้ใช้
    public IActionResult DeleteUser(int UID)
    {
        var role = HttpContext.Session.GetString("RoleName");
        if (role != "admin") return RedirectToAction("Home", "Home");

        var user = (from u in _db.Users where u.UsId == UID select u).FirstOrDefault();
        if (user != null) 
        {
            _db.RemoveRange(user);
            _db.SaveChanges();
        }

        return RedirectToAction("UserList", "Account");
    }

    // แก้ไขผู้ใช้ 
    public IActionResult EditUser(int UID)
    {
        var role = HttpContext.Session.GetString("RoleName");
        if (role != "admin") return RedirectToAction("Home", "Home");

        var user = _db.Users.FirstOrDefault(u => u.UsId == UID);
        if (user == null) return RedirectToAction("UserList");

        var model = new RegisterUserViewModels
        {
            UsId = user.UsId,
            Name = user.UsName,
            Lastname = user.UsLastname,
            Phone = user.UsPhone,
            RoleId = user.RoleId
        };

        ViewBag.Roles = _db.Roles.Select(r => new Project.ViewModels.RoleViewModels
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName
        }).ToList();

        return View(model);
    }

    // บันทึกการแก้ไขผู้ใช้
    [HttpPost]
    public IActionResult EditUser(RegisterUserViewModels data)
    {
        var user = _db.Users.FirstOrDefault(u => u.UsId == data.UsId);

        if (user != null)
        {
            user.UsName = data.Name;
            user.UsLastname = data.Lastname;
            user.UsPhone = data.Phone;
            user.RoleId = data.RoleId;

            _db.SaveChanges();
        }

        return RedirectToAction("UserList");
    }

    // (Customer Only)

    // แสดงข้อมูลโปรไฟล์ผู้ใช้
    [HttpGet]
    public IActionResult Profile()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = _db.Users.FirstOrDefault(u => u.UsId == userId);
        if (user == null) return RedirectToAction("Logout", "Account");

        var model = new RegisterUserViewModels
        {
            UsId = user.UsId,
            Name = user.UsName,
            Lastname = user.UsLastname,
            Phone = user.UsPhone,
            Address = user.UsAddress
        };

        return View(model);
    }

    // บันทึกการแก้ไขข้อมูลโปรไฟล์ผู้ใช้
    [HttpPost]
    public IActionResult Profile(RegisterUserViewModels data)
    {
        int? currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId == null || currentUserId != data.UsId)
        {
            return RedirectToAction("Login", "Account");
        }

        var user = _db.Users.FirstOrDefault(u => u.UsId == data.UsId);

        if (user != null)
        {
            user.UsName = data.Name;
            user.UsLastname = data.Lastname;
            user.UsPhone = data.Phone;
            user.UsAddress = data.Address;

            _db.SaveChanges();
            HttpContext.Session.SetString("UserName", user.UsName);
            ViewBag.Success = "บันทึกข้อมูลเรียบร้อยแล้ว";
        }

        return View(data);
    }

    public IActionResult Otp()
    {
        return View();
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    public IActionResult OtpForgotPassword()
    {
        return View();
    }

    public IActionResult ResetPassword()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
