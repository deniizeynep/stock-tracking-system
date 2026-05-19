using Avalonia.Controls;
using Avalonia.Interactivity;
using Dapper;
using StokTakipSistemi.Data;
using StokTakipSistemi.Models;
using System;

namespace StokTakipSistemi;

public partial class GuncelleWindow : Window
{
    private Urun _seciliUrun;
    private Database _db;

    // 1. Avalonia'nın kızmaması için gereken boş başlatıcı
    public GuncelleWindow()
    {
        InitializeComponent();
    }

    // 2. Ana ekrandan ürün verisi alarak açılan asıl başlatıcı
    public GuncelleWindow(Urun urun)
    {
        InitializeComponent();
        
        _seciliUrun = urun;
        _db = new Database(); // Veritabanı bağlantımızı hazırlıyoruz

        // Ekran açıldığı an kutuları mevcut bilgilerle dolduruyoruz
        txtBarkod.Text = _seciliUrun.Barkod;
        txtUrunAdi.Text = _seciliUrun.UrunAdi;
        txtKategori.Text = _seciliUrun.Kategori;
        txtFiyat.Text = _seciliUrun.Fiyat.ToString();
        txtStok.Text = _seciliUrun.StokMiktari.ToString();

        btnKaydet.Click += BtnKaydet_Click;
    }

    private void BtnKaydet_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Kutulardaki yeni verileri alıyoruz
            _seciliUrun.Barkod = txtBarkod.Text;
            _seciliUrun.UrunAdi = txtUrunAdi.Text;
            _seciliUrun.Kategori = txtKategori.Text;
            _seciliUrun.Fiyat = Convert.ToDouble(txtFiyat.Text);
            _seciliUrun.StokMiktari = Convert.ToInt32(txtStok.Text);

            // Veritabanında güncelleme işlemini (UPDATE) yapıyoruz
            using (var connection = _db.GetConnection())
            {
                string sql = @"UPDATE Urunler 
                               SET Barkod = @Barkod, UrunAdi = @UrunAdi, Kategori = @Kategori, Fiyat = @Fiyat, StokMiktari = @StokMiktari 
                               WHERE Id = @Id";
                connection.Execute(sql, _seciliUrun);
            }

            // İşlem başarıyla bitince bu pencereyi kapatıyoruz
            this.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Güncelleme hatası: " + ex.Message);
        }
    }
}