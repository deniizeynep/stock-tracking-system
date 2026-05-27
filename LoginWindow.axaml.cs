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
            connection.Execute("DROP TABLE IF EXISTS Kullanicilar;");

            string tabloSql = @"CREATE TABLE IF NOT EXISTS Kullanicilar (
                                  Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                  KullaniciAdi TEXT UNIQUE,
                                  Sifre TEXT,
                                  Yetki TEXT
                                );";
            connection.Execute(tabloSql);

            int adminSayisi = connection.QuerySingle<int>("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = 'admin'");
            if (adminSayisi == 0)
                connection.Execute("INSERT INTO Kullanicilar (KullaniciAdi, Sifre, Yetki) VALUES ('admin', '1234', 'Admin')");

            int personelSayisi = connection.QuerySingle<int>("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = 'personel'");
            if (personelSayisi == 0)
                connection.Execute("INSERT INTO Kullanicilar (KullaniciAdi, Sifre, Yetki) VALUES ('personel', '1234', 'Personel')");
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
            string sql = "SELECT Yetki FROM Kullanicilar WHERE KullaniciAdi = @kAdi AND Sifre = @sifre";
            string yetki = connection.QueryFirstOrDefault<string>(sql, new { kAdi, sifre });

            if (!string.IsNullOrEmpty(yetki))
            {
                var mainWindow = new MainWindow(yetki);
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