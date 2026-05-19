using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StokTakipSistemi;

public partial class OzelliklerWindow : Window
{
    // 1. Avalonia'nın arka planda hata vermemesi için gereken boş başlatıcı
    public OzelliklerWindow()
    {
        InitializeComponent();
    }

    // 2. Bizim kategori verisi göndererek açtığımız asıl başlatıcı
    public OzelliklerWindow(string kategori)
    {
        InitializeComponent();
        btnOzellikKaydet.Click += BtnOzellikKaydet_Click;

        // Gelen kategoriye göre başlığı değiştiriyoruz
        txtBaslik.Text = $"{kategori} Özellikleri";

        // Gelen kategori adına göre sadece ilgili kutuları görünür yapıyoruz
        if (kategori == "Bilgisayar") pnlBilgisayar.IsVisible = true;
        else if (kategori == "Telefon") pnlTelefon.IsVisible = true;
        else if (kategori == "Televizyon") pnlTelevizyon.IsVisible = true;
        else if (kategori == "Ev Aletleri") pnlEvAletleri.IsVisible = true;
    }

    private void BtnOzellikKaydet_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}