using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using WebApplication222.Models;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
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

        public IActionResult Index()
        {
            var username = _httpContextAccessor.HttpContext?.Session.GetString("Username");

            if (username?.EndsWith("@admin") == true)
            {
                return View("Index", GetUsers());
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

            if (reader.Read()) // May nahanap bang user?
            {
                string storedPassword = reader.GetString("password");

                Console.WriteLine("Entered Password: " + password);
                Console.WriteLine("Stored Password: " + storedPassword);

                if (password == storedPassword) // Direktang pag-compare ng password
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

            string hashedPassword = ComputeSha256Hash(user.Password);

            using var cmd = new MySqlCommand("INSERT INTO user(Username, Password) VALUES (@Username, @Password)", conn);
            cmd.Parameters.AddWithValue("@Username", user.Username);
            cmd.Parameters.AddWithValue("@Password", hashedPassword);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult UpdateUser(User user)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            string hashedPassword = ComputeSha256Hash(user.Password);

            using var cmd = new MySqlCommand("UPDATE user SET password = @password WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@password", hashedPassword);
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult DeleteUser(int id)
        {
            if (_httpContextAccessor.HttpContext?.Session.GetString("Username") != "admin@admin")
            {
                return Unauthorized();
            }

            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM user WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
