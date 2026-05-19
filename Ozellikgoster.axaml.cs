using Avalonia.Controls;
using StokTakipSistemi.Models;

namespace StokTakipSistemi;

public partial class OzellikGosterWindow : Window
{
    public OzellikGosterWindow() { InitializeComponent(); }

    public OzellikGosterWindow(Urun seciliUrun)
    {
        InitializeComponent();
        
        txtUrunBaslik.Text = seciliUrun.UrunAdi + " Özellikleri";
        
        if (string.IsNullOrEmpty(seciliUrun.Ozellikler))
        {
            txtOzellikler.Text = "Bu ürün için herhangi bir teknik özellik girilmemiştir.";
        }
        else
        {
            txtOzellikler.Text = "Kategori: " + seciliUrun.Kategori + "\n\n" +
                                 "--- TEKNİK DETAYLAR ---\n" + 
                                 seciliUrun.Ozellikler;
        }

        btnKapat.Click += (s, e) => this.Close();
    }
}