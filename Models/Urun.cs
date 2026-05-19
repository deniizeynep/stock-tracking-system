namespace StokTakipSistemi.Models;

public class Urun
{
    public int Id { get; set; }
    public string Barkod { get; set; } = "";
    public string UrunAdi { get; set; } = "";
    public string Kategori { get; set; } = "";
    public double Fiyat { get; set; }
    public int StokMiktari { get; set; }
    public string? Ozellikler { get; set; } 
}