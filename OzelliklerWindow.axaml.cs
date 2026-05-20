using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StokTakipSistemi;

public partial class OzelliklerWindow : Window
{
    public string ToplananOzellikler { get; private set; } = "";
    private string _kategori = "";

    public OzelliklerWindow() { InitializeComponent(); }

    public OzelliklerWindow(string kategori)
    {
        InitializeComponent();
        _kategori = kategori;
        
        if (txtBaslik != null)
            txtBaslik.Text = $"{kategori} Detayları";

        switch (kategori)
        {
            case "Bilgisayar":
                pnlBilgisayar.IsVisible = true;
                break;
            case "Telefon":
                pnlTelefon.IsVisible = true;
                break;
            case "Televizyon & Ses Sistemleri":
                pnlTelevizyon.IsVisible = true;
                break;
            case string k when k.Contains("Beyaz Eşya"):
                pnlEvAletleri.IsVisible = true;
                lblDinamik1.Text = "Enerji Sınıfı:";
                lblDinamik2.Text = "Kapasite (KG / Litre):";
                txtGuc.Watermark = "Örn: A+++, E Sınıfı";
                txtHacim.Watermark = "Örn: 9 KG, 450 Litre";
                break;
            case string k when k.Contains("Süpürge"):
                pnlEvAletleri.IsVisible = true;
                lblDinamik1.Text = "Emiş Gücü (Pa/Watt):";
                lblDinamik2.Text = "Hazne Kapasitesi:";
                txtGuc.Watermark = "Örn: 2500 Pa, 700W";
                txtHacim.Watermark = "Örn: 0.5L, 2L Toz Torbalı";
                break;
            case string k when k.Contains("İklimlendirme"):
                pnlEvAletleri.IsVisible = true;
                lblDinamik1.Text = "BTU Değeri:";
                lblDinamik2.Text = "İnverter Teknolojisi:";
                txtGuc.Watermark = "Örn: 12000 BTU, 18000 BTU";
                txtHacim.Watermark = "Örn: Var, Yok";
                break;
            case string k when k.Contains("Kişisel Bakım"):
                pnlEvAletleri.IsVisible = true;
                lblDinamik1.Text = "Kullanım Süresi (Dk):";
                lblDinamik2.Text = "Su Geçirmezlik:";
                txtGuc.Watermark = "Örn: 60 Dakika, 90 Dakika";
                txtHacim.Watermark = "Örn: IPX7, Var, Yok";
                break;
            default: 
                pnlEvAletleri.IsVisible = true;
                lblDinamik1.Text = "Güç Tüketimi (Watt):";
                lblDinamik2.Text = "Hazne/Hacim:";
                break;
        }

        btnOzellikKaydet.Click += BtnOzellikKaydet_Click;
    }

    private void BtnOzellikKaydet_Click(object? sender, RoutedEventArgs e)
    {
        string detaylar = $"Marka: {txtMarka.Text}\nModel: {txtModel.Text}\n";

        if (_kategori == "Bilgisayar")
            detaylar += $"İşlemci: {txtIslemci.Text}\nRAM: {txtRam.Text}";
        else if (_kategori == "Telefon")
            detaylar += $"Kamera: {txtKamera.Text}\nBatarya: {txtBatarya.Text}";
        else if (_kategori.Contains("Televizyon"))
            detaylar += $"Çözünürlük: {txtCozunurluk.Text}\nEkran: {txtEkranBoyutu.Text}";
        else 
        {
            detaylar += $"{lblDinamik1.Text} {txtGuc.Text}\n{lblDinamik2.Text} {txtHacim.Text}";
        }

        ToplananOzellikler = detaylar;
        this.Close();
    }
}