using Avalonia.Controls;
using Avalonia.Interactivity;
using StokTakipSistemi.Models;

namespace StokTakipSistemi;

public partial class OzellikGosterWindow : Window
{
    public OzellikGosterWindow() { InitializeComponent(); }

    public OzellikGosterWindow(Urun seciliUrun)
    {
        InitializeComponent();
        
        // Ürün adını başlığa yazdırıyoruz
        txtUrunBaslik.Text = seciliUrun.UrunAdi + " Özellikleri";
        
        // Şimdilik buraya veritabanından gelecek özellikleri bağlayacağız
        // (Bir sonraki adımda veritabanına özellik kaydetmeyi eklememiz gerekecek)
        txtOzellikler.Text = "Kategori: " + seciliUrun.Kategori + "\n\n" +
                             "Bu ürünün teknik detayları veritabanından buraya yüklenecektir...";

        btnKapat.Click += (s, e) => this.Close();
    }
}