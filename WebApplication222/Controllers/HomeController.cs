using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using WebApplication222.Models;

namespace WebApplication222.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(GetUsers());
        }

        public IActionResult Privacy()
        {
            return View();
        }
        private string connStr = "Server=localhost;Database=account;User=root;Password=;";
        
        public IActionResult InsertUser(account account)
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("INSERT INTO user(username, password) VALUES (@username, @password)", conn);
            cmd.Parameters.AddWithValue("@username", account.username);
            cmd.Parameters.AddWithValue("@password", account.password);
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
             
        }

        public List<User> GetUsers() {
            var user = new List<User>();
            using var conn = new MySqlConnection(connStr);
            conn.Open();

            using var cmd = new MySqlCommand("SELECT * FROM user", conn);  
            using var reader = cmd.ExecuteReader();

            while (reader.Read()){
                user.Add(new User 
                { 
                Id = reader.GetInt32("id"),
                Username = reader.GetString("username"),
                Password = reader.GetString("password")
                });
            }    
            return user;
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
