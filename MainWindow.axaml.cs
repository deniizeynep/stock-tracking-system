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
    private string _bekleyenIslem = "";

    public MainWindow(string kullaniciYetkisi = "Admin")
    {
        InitializeComponent();

        _aktifKullaniciYetkisi = kullaniciYetkisi;
        _db = new Database();

        VeritabaniniHazirla();

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

    private void VeritabaniniHazirla()
    {
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
            catch { }
        }
    }
    private void TumSayfalariGizle()
    {
        sayfaStok.IsVisible = false;
        sayfaFinans.IsVisible = false;
        sayfaPersonel.IsVisible = false;
        sayfaAyarlar.IsVisible = false;
    }

    private void BtnMenuStok_Click(object? sender, RoutedEventArgs e)
    {
        TumSayfalariGizle();
        sayfaStok.IsVisible = true;
    }

    private void BtnMenuFinans_Click(object? sender, RoutedEventArgs e)
    {
        TumSayfalariGizle();
        sayfaFinans.IsVisible = true; 
        FinansalVerileriHesapla(); 
    }

    private void BtnMenuPersonel_Click(object? sender, RoutedEventArgs e)
    {
        if (_aktifKullaniciYetkisi == "Personel")
        {
            MesajGoster("Bu modülü görme yetkiniz yok!", true);
            return;
        }
        TumSayfalariGizle();
        sayfaPersonel.IsVisible = true;
        PersonelleriListele();
    }

    private void BtnMenuAyarlar_Click(object? sender, RoutedEventArgs e)
    {
        TumSayfalariGizle();
        sayfaAyarlar.IsVisible = true;
    }
    private async void UrunleriListele()
    {
        await KurlariGuncelle();

        using (var connection = _db.GetConnection())
        {
            string sql = "SELECT * FROM Urunler";
            var urunler = connection.Query<Urun>(sql).ToList();
            
            double toplamMaliyetTL = urunler.Sum(u => u.Fiyat * u.StokMiktari);
            
            txtDolarKarsiligi.Text = $"Depo Değeri ($): {(toplamMaliyetTL / _usdKuru):N2}";
            txtEuroKarsiligi.Text = $"Depo Değeri (€): {(toplamMaliyetTL / _eurKuru):N2}";

            lstUrunler.ItemsSource = urunler;
        }
        
        brdFinansal.IsVisible = true; 
        KritikStokKontrol();
    }

    private void BtnKaydet_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtBarkod.Text) || string.IsNullOrWhiteSpace(txtUrunAdi.Text) || 
            string.IsNullOrWhiteSpace(txtFiyat.Text) || string.IsNullOrWhiteSpace(txtStok.Text) || 
            cmbKategori.SelectedIndex == -1)
        {
            txtFormUyari.Text = "⚠️ Lütfen tüm alanları doldurunuz!";
            return;
        }

        if (string.IsNullOrWhiteSpace(_geciciOzellikler))
        {
            txtFormUyari.Text = "⚠️ Lütfen ürünü kaydetmeden önce detayları doldurun!";
            return; 
        }

        if (!double.TryParse(txtFiyat.Text, out double fiyat) || !int.TryParse(txtStok.Text, out int stok))
        {
            txtFormUyari.Text = "⚠️ Hata: Fiyat ve Stok alanlarına sadece sayı girmelisiniz!";
            return;
        }

        txtFormUyari.Text = ""; 
        _bekleyenIslem = "Kaydet";
        txtOnayMesaji.Text = "Bu ürünü sisteme kaydetmek istediğinize emin misiniz?";
        gridOnay.IsVisible = true;
    }

    private void BtnSil_Click(object? sender, RoutedEventArgs e)
    {
        if (lstUrunler.SelectedItem is Urun seciliUrun)
        {
            _bekleyenIslem = "Sil";
            txtOnayMesaji.Text = $"{seciliUrun.UrunAdi} ürününü tamamen silmek istediğinize emin misiniz?";
            gridOnay.IsVisible = true;
        }
        else
        {
            MesajGoster("Lütfen silmek için listeden bir ürün seçin!", true);
        }
    }

    private void LstUrunler_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (lstUrunler.SelectedItem is Urun seciliUrun)
        {
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
            BtnTemizle_Click(null, null);
        }
    }

    private void BtnTemizle_Click(object? sender, RoutedEventArgs e)
    {
        txtBarkod.Text = ""; txtUrunAdi.Text = ""; txtFiyat.Text = ""; txtStok.Text = "";
        cmbKategori.SelectedIndex = -1; txtFormUyari.Text = ""; 

        txtBarkod.IsEnabled = true; txtUrunAdi.IsEnabled = true;
        txtFiyat.IsEnabled = true; txtStok.IsEnabled = true;
        cmbKategori.IsEnabled = true; btnKaydet.IsEnabled = true; btnDetayGir.IsEnabled = true;

        lstUrunler.SelectedItem = null;
        _geciciOzellikler = ""; 
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
            else txtUyari.Text = "";
        }
    }

    private void MenuItemFiltre_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            string filtre = menuItem.Header?.ToString() ?? "";
            using (var connection = _db.GetConnection())
            {
                string sql = "SELECT * FROM Urunler"; 

                if (filtre.Contains("Kritik Stok")) sql = "SELECT * FROM Urunler WHERE StokMiktari < 10";
                else if (filtre.Contains("Bilgisayar")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Bilgisayar'";
                else if (filtre.Contains("Telefon")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Telefon'";
                else if (filtre.Contains("Televizyon")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Televizyon'";
                else if (filtre.Contains("Beyaz Eşya")) sql = "SELECT * FROM Urunler WHERE Kategori = 'Beyaz Eşya'";

                lstUrunler.ItemsSource = connection.Query<Urun>(sql).ToList();
            }
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

    private void FinansalVerileriHesapla()
    {
        using (var connection = _db.GetConnection())
        {
            var urunler = connection.Query<Urun>("SELECT * FROM Urunler").ToList();

            double toplamVarlik = urunler.Sum(u => u.Fiyat * u.StokMiktari);
            txtFinansToplamDeger.Text = $"{toplamVarlik:N2} TL";

            var kategoriAnalizi = urunler
                .GroupBy(u => string.IsNullOrWhiteSpace(u.Kategori) ? "Kategorisiz" : u.Kategori)
                .Select(g => new KategoriAnalizModel
                {
                    KategoriAdi = g.Key,
                    Adet = g.Sum(u => u.StokMiktari),
                    ToplamDeger = g.Sum(u => u.Fiyat * u.StokMiktari)
                })
                .OrderByDescending(k => k.ToplamDeger).ToList();

            lstKategoriAnaliz.ItemsSource = kategoriAnalizi;
        }
    }
    private void PersonelleriListele()
    {
        using (var connection = _db.GetConnection())
        {
            var kullanicilar = connection.Query<KullaniciModel>("SELECT * FROM Kullanicilar").ToList();
            lstPersoneller.ItemsSource = kullanicilar;
        }
    }

    private void BtnPersonelEkle_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtYeniKullanici.Text) || string.IsNullOrWhiteSpace(txtYeniSifre.Text) || cmbYeniYetki.SelectedIndex == -1)
        {
            MesajGoster("Lütfen kullanıcı adı, şifre ve yetki alanlarını doldurun!", true);
            return;
        }

        string seciliYetki = (cmbYeniYetki.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Personel";

        using (var connection = _db.GetConnection())
        {
            int varMi = connection.QuerySingle<int>("SELECT COUNT(*) FROM Kullanicilar WHERE KullaniciAdi = @KAdi", new { KAdi = txtYeniKullanici.Text });
            if (varMi > 0)
            {
                MesajGoster("Bu kullanıcı adı zaten sistemde mevcut!", true);
                return;
            }

            connection.Execute("INSERT INTO Kullanicilar (KullaniciAdi, Sifre, Yetki) VALUES (@KAdi, @Sifre, @Yetki)",
                new { KAdi = txtYeniKullanici.Text, Sifre = txtYeniSifre.Text, Yetki = seciliYetki });

            MesajGoster($"{txtYeniKullanici.Text} adlı {seciliYetki} sisteme eklendi!");
            
            txtYeniKullanici.Text = ""; txtYeniSifre.Text = ""; cmbYeniYetki.SelectedIndex = -1;
            PersonelleriListele();
        }
    }

    private void BtnPersonelSil_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is KullaniciModel silinecekKisi)
        {
            if (silinecekKisi.KullaniciAdi.ToLower() == "admin")
            {
                MesajGoster("Güvenlik: Ana 'admin' hesabı sistemden silinemez!", true);
                return;
            }

            using (var connection = _db.GetConnection())
            {
                connection.Execute("DELETE FROM Kullanicilar WHERE Id = @Id", new { Id = silinecekKisi.Id });
                MesajGoster($"🗑️ {silinecekKisi.KullaniciAdi} hesabı sistemden kaldırıldı!");
                PersonelleriListele();
            }
        }
    }
    private void YetkiAyarlariniUygula()
    {
        if (_aktifKullaniciYetkisi == "Personel")
        {
            if (btnSil != null) btnSil.IsVisible = false;
            if (btnExcelAktar != null) btnExcelAktar.IsVisible = false;
            if (this.FindControl<Border>("brdFinansal") is Border b) b.IsVisible = false;
        }
    }

    private void BtnVeritabaniniYedekle_Click(object? sender, RoutedEventArgs e)
    {
        if (_aktifKullaniciYetkisi == "Personel")
        {
            MesajGoster("Güvenlik: Personel yetkisi ile veritabanı yedeği alınamaz!", true);
            return; 
        }
        try
        {
            string kaynakDosya = "StokTakip.db"; 
            if (!System.IO.File.Exists(kaynakDosya)) kaynakDosya = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StokTakip.db");
            if (!System.IO.File.Exists(kaynakDosya)) { MesajGoster("Hata: StokTakip.db bulunamadı!", true); return; }

            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string hedefKlasor = System.IO.Path.Combine(homeDir, "Downloads");
            if (!System.IO.Directory.Exists(hedefKlasor)) hedefKlasor = System.IO.Path.Combine(homeDir, "İndirilenler");
            if (!System.IO.Directory.Exists(hedefKlasor)) hedefKlasor = homeDir;

            string yedekDosyaAdi = $"StokYedek_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            System.IO.File.Copy(kaynakDosya, System.IO.Path.Combine(hedefKlasor, yedekDosyaAdi), true);
            
            MesajGoster($"Yedekleme başarılı: {yedekDosyaAdi}");
        }
        catch (Exception ex) { MesajGoster($"Yedekleme Hatası: {ex.Message}", true); }
    }

    private void BtnExcelAktar_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string hedefKlasor = System.IO.Path.Combine(homeDir, "Downloads");
            if (!System.IO.Directory.Exists(hedefKlasor)) hedefKlasor = System.IO.Path.Combine(homeDir, "İndirilenler");
            if (!System.IO.Directory.Exists(hedefKlasor)) hedefKlasor = homeDir;

            string dosyaAdi = $"StokRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            using (var connection = _db.GetConnection())
            {
                var urunler = connection.Query<Urun>("SELECT * FROM Urunler").ToList();
                using (var writer = new System.IO.StreamWriter(System.IO.Path.Combine(hedefKlasor, dosyaAdi), false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("Barkod;Urun Adi;Kategori;Fiyat;Stok Miktari;Ozellikler");
                    foreach (var urun in urunler)
                    {
                        string temizOzellik = urun.Ozellikler?.Replace("\n", " ").Replace(";", ",") ?? "";
                        writer.WriteLine($"{urun.Barkod};{urun.UrunAdi};{urun.Kategori};{urun.Fiyat};{urun.StokMiktari};{temizOzellik}");
                    }
                }
            }
            MesajGoster($"Rapor başarıyla kaydedildi: {dosyaAdi}");
        }
        catch (Exception ex) { MesajGoster($"Excel Hatası: {ex.Message}", true); }
    }

    private void BtnCikisYap_Click(object? sender, RoutedEventArgs e)
    {
        _bekleyenIslem = "Cikis";
        txtOnayMesaji.Text = "Oturumu kapatıp giriş ekranına dönmek istediğinize emin misiniz?";
        gridOnay.IsVisible = true;
    }

    private async void MesajGoster(string mesaj, bool hataMi = false)
    {
        txtToastMesaj.Text = mesaj;
        txtToastSimge.Text = hataMi ? "❌" : "✅";
        brdToast.BorderBrush = Avalonia.Media.SolidColorBrush.Parse(hataMi ? "#f43f5e" : "#10b981");
        
        brdToast.IsVisible = true;
        brdToast.Opacity = 1;

        await System.Threading.Tasks.Task.Delay(3000);
        brdToast.IsVisible = false;
    }

    private void BtnOnayEvet_Click(object? sender, RoutedEventArgs e)
    {
        gridOnay.IsVisible = false;

        using (var connection = _db.GetConnection())
        {
            if (_bekleyenIslem == "Kaydet")
            {
                try
                {
                    string seciliKategori = (cmbKategori.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Belirtilmedi";
                    var yeniUrun = new Urun { Barkod = txtBarkod.Text, UrunAdi = txtUrunAdi.Text, Kategori = seciliKategori, Fiyat = Convert.ToDouble(txtFiyat.Text), StokMiktari = Convert.ToInt32(txtStok.Text), Ozellikler = _geciciOzellikler };
                    connection.Execute("INSERT INTO Urunler (Barkod, UrunAdi, Kategori, Fiyat, StokMiktari, Ozellikler) VALUES (@Barkod, @UrunAdi, @Kategori, @Fiyat, @StokMiktari, @Ozellikler)", yeniUrun);

                    txtBarkod.Text = ""; txtUrunAdi.Text = ""; txtFiyat.Text = ""; txtStok.Text = ""; _geciciOzellikler = ""; cmbKategori.SelectedIndex = -1;
                    UrunleriListele();
                    MesajGoster("Ürün başarıyla sisteme eklendi!"); 
                }
                catch { MesajGoster("Kayıt başarısız oldu!", true); }
            }
            else if (_bekleyenIslem == "Sil")
            {
                if (lstUrunler.SelectedItem is Urun seciliUrun)
                {
                    connection.Execute("DELETE FROM Urunler WHERE Id = @Id", new { Id = seciliUrun.Id });
                    UrunleriListele();
                    MesajGoster("🗑️ Ürün başarıyla silindi!");
                }
            }
            else if (_bekleyenIslem == "Cikis")
            {
                new LoginWindow().Show();
                this.Close();
            }
        }
        _bekleyenIslem = ""; 
    }

    private void BtnOnayHayir_Click(object? sender, RoutedEventArgs e)
    {
        gridOnay.IsVisible = false; 
        _bekleyenIslem = "";
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

    public class KategoriAnalizModel
    {
        public string KategoriAdi { get; set; } = "";
        public int Adet { get; set; }
        public double ToplamDeger { get; set; }
    }

    public class KullaniciModel
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string Sifre { get; set; } = "";
        public string Yetki { get; set; } = "";
        public string YetkiRenk => Yetki == "Admin" ? "#28a745" : "#0d6efd"; 
    }
}