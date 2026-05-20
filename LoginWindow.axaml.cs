using Avalonia.Controls;
using Avalonia.Interactivity;
using Dapper;
using StokTakipSistemi.Data;
using System.Linq;

namespace StokTakipSistemi;

public partial class LoginWindow : Window
{
    private Database _db;

    public LoginWindow()
    {
        InitializeComponent();
        _db = new Database();
        
        VeritabaniniHazirla();
        
        btnGiris.Click += BtnGiris_Click;
    }

    private void VeritabaniniHazirla()
    {
        using (var connection = _db.GetConnection())
        {
            string tabloSql = @"CREATE TABLE IF NOT EXISTS Kullanicilar (
                                  Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                  KullaniciAdi TEXT UNIQUE,
                                  Sifre TEXT
                                );";
            connection.Execute(tabloSql);

            int adminSayisi = connection.QuerySingle<int>("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = 'admin'");
            if (adminSayisi == 0)
            {
                connection.Execute("INSERT INTO Kullanicilar (KullaniciAdi, Sifre) VALUES ('admin', '1234')");
            }
        }
    }

    private void BtnGiris_Click(object? sender, RoutedEventArgs e)
    {
        string kAdi = txtKullaniciAdi.Text ?? "";
        string sifre = txtSifre.Text ?? "";

        if (string.IsNullOrWhiteSpace(kAdi) || string.IsNullOrWhiteSpace(sifre))
        {
            txtUyari.Text = "⚠️ Lütfen tüm alanları doldurun!";
            return;
        }

        using (var connection = _db.GetConnection())
        {
            string sql = "SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @kAdi AND Sifre = @sifre";
            int sonuc = connection.QuerySingle<int>(sql, new { kAdi, sifre });

            if (sonuc > 0)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                txtUyari.Text = "❌ Hatalı kullanıcı adı veya şifre!";
                txtSifre.Text = ""; 
            }
        }
    }
}