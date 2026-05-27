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
    private string _aktifKullaniciYetkisi;

    public MainWindow(string kullaniciYetkisi = "Admin")
    {
        InitializeComponent();

        _aktifKullaniciYetkisi = kullaniciYetkisi;

        
        _db = new Database();

        using (var connection = _db.GetConnection())
        {
            string tabloYaratSql = @"CREATE TABLE IF NOT EXISTS Urunler (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        Barkod TEXT,
                                        UrunAdi TEXT,
                                        Kategori TEXT,
                                        Fiyat REAL,
                                        StokMiktari INTEGER,
                                        Ozellikler TEXT
                                     );";
            connection.Execute(tabloYaratSql);

            try { connection.Execute("ALTER TABLE Urunler ADD COLUMN Ozellikler TEXT;"); }
            catch {  }
        }

        btnKaydet.Click += BtnKaydet_Click;
        btnSil.Click += BtnSil_Click;
        btnDetayGir.Click += BtnDetayGir_Click;
        btnTemizle.Click += BtnTemizle_Click;
        lstUrunler.SelectionChanged += LstUrunler_SelectionChanged;
        btnTemaDegistir.Click += BtnTemaDegistir_Click;

        YetkiAyarlariniUygula();
        UrunleriListele();
        KritikStokKontrol();
    }

  private void BtnKaydet_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtBarkod.Text) || 
            string.IsNullOrWhiteSpace(txtUrunAdi.Text) || 
            string.IsNullOrWhiteSpace(txtFiyat.Text) || 
            string.IsNullOrWhiteSpace(txtStok.Text) || 
            cmbKategori.SelectedIndex == -1)
        {
            txtFormUyari.Text = "⚠️ Lütfen tüm alanları (Barkod, Ürün Adı, Kategori, Fiyat, Stok Miktarı) doldurunuz!";
            return;
        }

        if (string.IsNullOrWhiteSpace(_geciciOzellikler))
        {
            txtFormUyari.Text = "⚠️ Lütfen ürünü kaydetmeden önce 'Teknik Özellikleri Gir' butonundan detayları doldurun!";
            return; 
        }

        try
        {
            string seciliKategori = (cmbKategori.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Belirtilmedi";

            var yeniUrun = new Urun
            {
                Barkod = txtBarkod.Text,
                UrunAdi = txtUrunAdi.Text,
                Kategori = seciliKategori, 
                Fiyat = Convert.ToDouble(txtFiyat.Text),
                StokMiktari = Convert.ToInt32(txtStok.Text),
                Ozellikler = _geciciOzellikler 
            };

            using (var connection = _db.GetConnection())
            {
                string sql = @"INSERT INTO Urunler (Barkod, UrunAdi, Kategori, Fiyat, StokMiktari, Ozellikler) 
                               VALUES (@Barkod, @UrunAdi, @Kategori, @Fiyat, @StokMiktari, @Ozellikler)";
                connection.Execute(sql, yeniUrun);
            }

            txtBarkod.Text = "";
            txtUrunAdi.Text = "";
            txtFiyat.Text = "";
            txtStok.Text = "";
            _geciciOzellikler = ""; 
            txtFormUyari.Text = ""; 

            UrunleriListele();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Kayıt sırasında bir hata oluştu: " + ex.Message);
        }
    }
    private void BtnTemizle_Click(object? sender, RoutedEventArgs e)
    {
        txtBarkod.Text = "";
        txtUrunAdi.Text = "";
        txtFiyat.Text = "";
        txtStok.Text = "";
        cmbKategori.SelectedIndex = -1; 
        txtFormUyari.Text = ""; // Temizle butonuna basılınca uyarı yazısını da temizliyoruz

        txtBarkod.IsEnabled = true;
        txtUrunAdi.IsEnabled = true;
        txtFiyat.IsEnabled = true;
        txtStok.IsEnabled = true;
        cmbKategori.IsEnabled = true;
        btnKaydet.IsEnabled = true;

        lstUrunler.SelectedItem = null;
        _geciciOzellikler = ""; 
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
            // --- 1. DURUM: BİR ÜRÜN SEÇİLDİYSE KUTULARI DOLDUR VE KİLİTLE ---
            txtBarkod.Text = seciliUrun.Barkod;
            txtUrunAdi.Text = seciliUrun.UrunAdi;
            txtFiyat.Text = seciliUrun.Fiyat.ToString();
            txtStok.Text = seciliUrun.StokMiktari.ToString();

            foreach (ComboBoxItem item in cmbKategori.Items)
            {
                if (item.Content?.ToString() == seciliUrun.Kategori)
                {
                    cmbKategori.SelectedItem = item;
                    break;
                }
            }

            txtBarkod.IsEnabled = false; txtUrunAdi.IsEnabled = false;
            txtFiyat.IsEnabled = false; txtStok.IsEnabled = false;
            cmbKategori.IsEnabled = false; btnKaydet.IsEnabled = false; 
            btnDetayGir.IsEnabled = false; 
            
            txtFormUyari.Text = ""; 
        }
        else
        {
            // --- 2. DURUM: SEÇİM İPTAL EDİLDİYSE (veya Çöp Kutusuna basıldıysa) HER ŞEYİ TEMİZLE VE AÇ ---
            txtBarkod.Text = ""; txtUrunAdi.Text = ""; txtFiyat.Text = ""; txtStok.Text = "";
            cmbKategori.SelectedIndex = -1; 
            txtFormUyari.Text = ""; 

            txtBarkod.IsEnabled = true; txtUrunAdi.IsEnabled = true;
            txtFiyat.IsEnabled = true; txtStok.IsEnabled = true;
            cmbKategori.IsEnabled = true; btnKaydet.IsEnabled = true;
            btnDetayGir.IsEnabled = true;

            _geciciOzellikler = ""; 
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

        _geciciOzellikler = ozellikPenceresi.ToplananOzellikler;
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

    
    private void BtnTemaDegistir_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            if (Application.Current.RequestedThemeVariant == ThemeVariant.Dark)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                btnTemaDegistir.Content = "🌙"; 
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                btnTemaDegistir.Content = "☀️"; 
            }
        }
    }
    private void MenuItemFiltre_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            string filtre = menuItem.Header?.ToString() ?? "";
            
            using (var connection = _db.GetConnection())
            {
                string sql = "SELECT * FROM Urunler"; // Varsayılan: Tüm Ürünler

                if (filtre.Contains("Kritik Stok"))
                {
                    sql = "SELECT * FROM Urunler WHERE StokMiktari < 10";
                }
                else if (filtre.Contains("Bilgisayar")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Bilgisayar'";
                else if (filtre.Contains("Telefon")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Telefon'";
                else if (filtre.Contains("Televizyon")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Televizyon'";
                else if (filtre.Contains("Beyaz Eşya")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Beyaz Eşya'";

                var filtrelenmisUrunler = connection.Query<Urun>(sql).ToList();
                lstUrunler.ItemsSource = filtrelenmisUrunler;
            }
        }
    }

    private void BtnExcelAktar_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string masaustuYolu = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string dosyaAdi = $"StokRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string tamYol = System.IO.Path.Combine(masaustuYolu, dosyaAdi);

            using (var connection = _db.GetConnection())
            {
                var urunler = connection.Query<Urun>("SELECT * FROM Urunler").ToList();
                
                using (var writer = new System.IO.StreamWriter(tamYol, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Barkod;Urun Adi;Kategori;Fiyat;Stok Miktari;Ozellikler");
                    
                    foreach (var urun in urunler)
                    {
                        string temizOzellik = urun.Ozellikler?.Replace("\n", " ").Replace(";", ",") ?? "Belirtilmedi";
                        string satir = $"{urun.Barkod};{urun.UrunAdi};{urun.Kategori};{urun.Fiyat};{urun.StokMiktari};{temizOzellik}";
                        writer.WriteLine(satir);
                    }
                }
            }
            
            txtUyari.Foreground = Avalonia.Media.SolidColorBrush.Parse("#0dcaf0");
            txtUyari.Text = $"✅ Rapor başarıyla masaüstüne kaydedildi:\n{dosyaAdi}";
        }
        catch (Exception ex)
        {
            txtUyari.Foreground = Avalonia.Media.SolidColorBrush.Parse("#ff6b6b");
            txtUyari.Text = $"❌ Excel'e aktarılırken hata oluştu: {ex.Message}";
        }
    }
   private void YetkiAyarlariniUygula()
    {
        if (_aktifKullaniciYetkisi == "Personel")
        {
            // Personel ürün silemez ve rapor çekemez
            if (btnSil != null) btnSil.IsVisible = false;
            if (btnExcelAktar != null) btnExcelAktar.IsVisible = false;

            // Personel deponun finansal değerlerini göremez
            if (txtToplamDeger != null) txtToplamDeger.IsVisible = false;
            if (this.FindControl<Border>("brdFinansal") is Border b) b.IsVisible = false;
        }
    }
}