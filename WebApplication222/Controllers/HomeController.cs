using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using WebApplication222.Models;
using System.Diagnostics;
using System.Collections.Generic;

namespace WebApplication222.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string connStr = "Server=localhost;Database=account;User=root;Password=;";

        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index(string searchQuery = "", int page = 1)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username");

            if (username?.EndsWith("@admin") == true)
            {
                int pageSize = 10;
                var users = GetUsersWithSearch(searchQuery, page, pageSize);
                int totalUsers = GetTotalUserCount(searchQuery);

                ViewBag.SearchQuery = searchQuery;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

                return View("Index", users);
            }
            else if (username?.EndsWith("@user") == true)
            {
                return RedirectToAction("UserHome");
            }
            return RedirectToAction("Login");
        }

        public IActionResult UserHome()
        {
            var users = GetUsers();
            return View("User", users);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT * FROM user WHERE username = @username", conn);
            cmd.Parameters.AddWithValue("@username", username);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string storedPassword = reader.GetString("password");

                if (password == storedPassword)
                {
                    _httpContextAccessor.HttpContext?.Session.SetString("Username", username);

                    if (username.EndsWith("@admin"))
                        return RedirectToAction("Index");
                    else
                        return RedirectToAction("UserHome");
                }
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        public IActionResult Logout()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult InsertUser(User user)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("INSERT INTO user(Username, Password) VALUES (@Username, @Password)", conn);
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult UpdateUser(User user)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("UPDATE user SET password = @password WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@password", user.Password);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Password updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username");

            if (string.IsNullOrEmpty(username) || !username.EndsWith("@admin"))
            {
                _logger.LogWarning("Unauthorized delete attempt.");
                return RedirectToAction("Login");
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM user WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "User deleted successfully!";
            return RedirectToAction("Index");
        }

        public List<User> GetUsers()
        {
            var users = new List<User>();
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT * FROM user", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32("id"),
                    Username = reader.GetString("username"),
                    Password = reader.GetString("password")
                });
            }
            return users;
        }

        public List<User> GetUsersWithSearch(string searchQuery, int page, int pageSize)
        {
            var users = new List<User>();
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            int offset = (page - 1) * pageSize;

            using var cmd = new MySqlCommand("SELECT * FROM user WHERE username LIKE @searchQuery ORDER BY id LIMIT @pageSize OFFSET @offset", conn);
            cmd.Parameters.AddWithValue("@searchQuery", "%" + searchQuery + "%");
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new User
                {
                    Id = reader.GetInt32("id"),
                    Username = reader.GetString("username"),
                    Password = reader.GetString("password")
                });
            }
            return users;
        }

        public int GetTotalUserCount(string searchQuery = "")
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM user WHERE username LIKE @searchQuery", conn);
            cmd.Parameters.AddWithValue("@searchQuery", "%" + searchQuery + "%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
