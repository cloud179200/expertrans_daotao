using ExpertransDaoTao.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ExpertransDaoTao.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly expertrans_liveContext _db;
        public HomeController(ILogger<HomeController> logger, expertrans_liveContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.FindFirst(ClaimTypes.Role).Value == "Admin") return Redirect("/Admin");
                else if (User.FindFirst(ClaimTypes.Role).Value == "Teacher") return Redirect("/Teacher");
                return Redirect("/Student");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [HttpGet("denied")]
        public IActionResult Denied()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Secure()
        {
            return View();
        }
        [HttpGet("login")]
        public IActionResult Login(string returnUrl)
        {
            //Url before redirect login
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            //Url before redirect login
            ViewData["ReturnUrl"] = returnUrl;
            //Hash password with md5
            string CreateMD5(string input)
            {
                // Use input string to calculate MD5 hash
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    // Convert the byte array to hexadecimal string
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
            string hash = CreateMD5(password);
            //Check user in database by username and password and get User info
            User user = new User();
            var query = _db.Students.Where(st => st.UserName == username && st.Password == hash);
            if (query.Count() > 0)
            {
                var student = query.FirstOrDefault<Students>();
                user.Id = (int)student.Id;
                user.UserName = student.UserName;
                user.Address = student.Address;
                user.Email = student.Email;
                user.Mobile = student.Mobile;
                user.Name = student.Name;
                user.Role = "Student";
            }

            var query2 = _db.Users.Where(ad => ad.UserName == username && ad.Password == hash);
            if (query2.Count<Users>() > 0)
            {
                var admin = query2.FirstOrDefault<Users>();
                user.Id = (int)admin.Id;
                user.UserName = admin.UserName;
                user.Address = admin.Address;
                user.Email = admin.Email;
                user.Mobile = admin.Mobile;
                user.Name = admin.Name;
                user.Role = "Admin";
            }
            var query3 = _db.Teachers.Where(ad => ad.UserName == username && ad.Password == hash);
            if (query3.Count() > 0)
            {
                var teacher = query3.FirstOrDefault<Teachers>();
                user.Id = (int)teacher.Id;
                user.UserName = teacher.UserName;
                user.Address = teacher.Address;
                user.Email = teacher.Email;
                user.Mobile = teacher.Mobile;
                user.Name = teacher.Name;
                user.Role = "Teacher";
            }

            //If has user
            if (!string.IsNullOrEmpty(user.UserName))
            {
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Authentication, user.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserName));
                claims.Add(new Claim(ClaimTypes.Name, user.Name));
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.Mobile));
                var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return Redirect("/");
            }
            TempData["Error"] = "Username or password incorrect. Please contact Admin";
            return View("login");
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
