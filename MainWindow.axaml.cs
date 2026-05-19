using Avalonia.Controls;
using Avalonia.Interactivity;
using Dapper;
using StokTakipSistemi.Data;
using StokTakipSistemi.Models;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http; 
using System.Threading.Tasks; 
using Avalonia;
using Avalonia.Styling;

namespace StokTakipSistemi;

public partial class MainWindow : Window
{

    
    private Database _db;

    private string _geciciOzellikler = "";
    private double _usdKuru = 1.0;
    private double _eurKuru = 1.0;

    public MainWindow()
    {
        InitializeComponent();
        
        _db = new Database();
        
        btnKaydet.Click += BtnKaydet_Click;
        btnSil.Click += BtnSil_Click;
        btnDetayGir.Click += BtnDetayGir_Click;
        btnTemizle.Click += BtnTemizle_Click;
    btnTemaDegistir.Click += BtnTemaDegistir_Click;

        UrunleriListele();
        KritikStokKontrol();
    }

    private void BtnKaydet_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Kullanıcının ComboBox'tan hangi kategoriyi seçtiğini alıyoruz
            string seciliKategori = (cmbKategori.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Belirtilmedi";

            var yeniUrun = new Urun
            {
                Barkod = txtBarkod.Text,
                UrunAdi = txtUrunAdi.Text,
                Kategori = seciliKategori, // Kategoriyi de ekledik
                Fiyat = Convert.ToDouble(txtFiyat.Text),
                StokMiktari = Convert.ToInt32(txtStok.Text)
            };

            using (var connection = _db.GetConnection())
            {
                string sql = @"INSERT INTO Urunler (Barkod, UrunAdi, Kategori, Fiyat, StokMiktari) 
                               VALUES (@Barkod, @UrunAdi, @Kategori, @Fiyat, @StokMiktari)";
                connection.Execute(sql, yeniUrun);
            }

            txtBarkod.Text = "";
            txtUrunAdi.Text = "";
            txtFiyat.Text = "";
            txtStok.Text = "";

            UrunleriListele();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Kayıt sırasında bir hata oluştu: " + ex.Message);
        }
    }

    private async void UrunleriListele()
    {
        await KurlariGuncelle();

        using (var connection = _db.GetConnection())
        {
            string sql = "SELECT * FROM Urunler";
            var urunler = connection.Query<Urun>(sql).ToList();
            
            double toplamMaliyetTL = urunler.Sum(u => u.Fiyat * u.StokMiktari);
            
            txtToplamDeger.Text = $"💰 Toplam Stok Değeri: {toplamMaliyetTL:N2} TL";
            txtDolarKarsiligi.Text = $"Depo Değeri ($): {(toplamMaliyetTL / _usdKuru):N2}";
            txtEuroKarsiligi.Text = $"Depo Değeri (€): {(toplamMaliyetTL / _eurKuru):N2}";

            lstUrunler.ItemsSource = urunler;
        }
        KritikStokKontrol();
    }

    private void BtnSil_Click(object? sender, RoutedEventArgs e)
    {
        if (lstUrunler.SelectedItem is Urun seciliUrun)
        {
            using (var connection = _db.GetConnection())
            {
                string sql = "DELETE FROM Urunler WHERE Id = @Id";
                connection.Execute(sql, new { Id = seciliUrun.Id });
            }

            UrunleriListele();
            KritikStokKontrol();
        }
    }

    private void LstUrunler_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (lstUrunler.SelectedItem is Urun seciliUrun)
        {
            // Verileri doldur
            txtBarkod.Text = seciliUrun.Barkod;
            txtUrunAdi.Text = seciliUrun.UrunAdi;
            txtFiyat.Text = seciliUrun.Fiyat.ToString();
            txtStok.Text = seciliUrun.StokMiktari.ToString();

            // SİHİRLİ DOKUNUŞ: Kutuları kilitliyoruz (Kullanıcı değiştiremez)
            txtBarkod.IsEnabled = false;
            txtUrunAdi.IsEnabled = false;
            txtFiyat.IsEnabled = false;
            txtStok.IsEnabled = false;
            cmbKategori.IsEnabled = false;
            btnKaydet.IsEnabled = false; // Kaydet butonunu da kapatıyoruz ki yanlışlıkla basmasın
        }
    }

    private void KritikStokKontrol()
    {
        using (var connection = _db.GetConnection())
        {
            string sql = "SELECT UrunAdi, StokMiktari FROM Urunler WHERE StokMiktari < 10";
            var kritikUrunler = connection.Query<Urun>(sql).ToList();

            if (kritikUrunler.Count > 0)
            {
                string isimler = string.Join(", ", kritikUrunler.Select(u => u.UrunAdi));
                txtUyari.Text = $"⚠️ DİKKAT! Kritik stok seviyesinin altında {kritikUrunler.Count} ürün var:\n{isimler}";
            }
            else
            {
                txtUyari.Text = "";
            }
        }
    }

    private void TxtArama_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string arananKelime = txtArama.Text ?? "";

        using (var connection = _db.GetConnection())
        {
            string sql = "SELECT * FROM Urunler WHERE UrunAdi LIKE @Aranan OR Barkod LIKE @Aranan";
            var filtrelenmisUrunler = connection.Query<Urun>(sql, new { Aranan = $"%{arananKelime}%" }).ToList();
            
            lstUrunler.ItemsSource = filtrelenmisUrunler;
        }
    }

    private async Task KurlariGuncelle()
    {
        try
        {
            using (var client = new HttpClient())
            {
                var xmlStr = await client.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");
                var xml = XDocument.Parse(xmlStr);

                _usdKuru = Convert.ToDouble(xml.Descendants("Currency").First(x => x.Attribute("CurrencyCode")?.Value == "USD").Element("ForexBuying")?.Value.Replace(".", ","));
                _eurKuru = Convert.ToDouble(xml.Descendants("Currency").First(x => x.Attribute("CurrencyCode")?.Value == "EUR").Element("ForexBuying")?.Value.Replace(".", ","));

                lblDolar.Text = $"USD: {_usdKuru:N2} TL";
                lblEuro.Text = $"EUR: {_eurKuru:N2} TL";
            }
        }
        catch { }
    }

    private async void BtnDetayGir_Click(object? sender, RoutedEventArgs e)
    {
        string seciliKategori = (cmbKategori.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var ozellikPenceresi = new OzelliklerWindow(seciliKategori);
        await ozellikPenceresi.ShowDialog(this);
    }
    private async void BtnOzellikGor_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Urun seciliUrun)
        {
            var ozellikPenceresi = new OzellikGosterWindow(seciliUrun);
            await ozellikPenceresi.ShowDialog(this);
        }
    }
    private async void BtnSatirGuncelle_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Urun seciliUrun)
        {
            var guncellePenceresi = new GuncelleWindow(seciliUrun);
            await guncellePenceresi.ShowDialog(this);
            
            UrunleriListele();
        }
    }
    private void BtnTemizle_Click(object? sender, RoutedEventArgs e)
    {
        txtBarkod.Text = "";
        txtUrunAdi.Text = "";
        txtFiyat.Text = "";
        txtStok.Text = "";
        cmbKategori.SelectedIndex = -1; 

        txtBarkod.IsEnabled = true;
        txtUrunAdi.IsEnabled = true;
        txtFiyat.IsEnabled = true;
        txtStok.IsEnabled = true;
        cmbKategori.IsEnabled = true;
        btnKaydet.IsEnabled = true;

        // Listeden seçimi kaldır
        lstUrunler.SelectedItem = null;
    }
private void BtnTemaDegistir_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            // Karanlıktaysa Aydınlığa (Light) çevir
            if (Application.Current.RequestedThemeVariant == ThemeVariant.Dark)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                btnTemaDegistir.Content = "🌙"; // Sadece ikon
            }
            // Aydınlıktaysa Karanlığa (Dark) çevir
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                btnTemaDegistir.Content = "☀️"; // Sadece ikon
            }
        }
    }
}