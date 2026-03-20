using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClickerGame
{
    public record Account(string Username, string PasswordHash);

    public class AccountsManager
    {
        readonly string path;
        List<Account> accounts = new();

        public AccountsManager(string dataPath = "Accounts/accounts.json")
        {
            path = dataPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Accounts");
            Load();
        }

        void Load()
        {
            if (!File.Exists(path)) { accounts = new List<Account>(); return; }
            try
            {
                var s = File.ReadAllText(path);
                accounts = JsonSerializer.Deserialize<List<Account>>(s) ?? new List<Account>();
            }
            catch
            {
                accounts = new List<Account>();
            }
        }

        void Save()
        {
            var s = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, s);
        }

        static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var b = Encoding.UTF8.GetBytes(password);
            var h = sha.ComputeHash(b);
            return Convert.ToHexString(h);
        }

        public bool Register(string username, string password, out string message)
        {
            username = username?.Trim() ?? "";
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) { message = "Username and password required"; return false; }
            if (accounts.Any(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase))) { message = "Username already exists"; return false; }
            var h = HashPassword(password);
            accounts.Add(new Account(username, h));
            Save();
            message = "Registered";
            return true;
        }

        public bool Authenticate(string username, string password)
        {
            var h = HashPassword(password);
            return accounts.Any(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase) && a.PasswordHash == h);
        }
    }
}
