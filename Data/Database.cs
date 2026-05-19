using Microsoft.Data.Sqlite;

namespace StokTakipSistemi.Data;

public class Database
{
    // Veritabanı dosyamızın adı ve bağlantı cümlesi
    private const string ConnectionString = "Data Source=StokTakip.db";

    // Programın veritabanına işlem yapmak için kullanacağı köprü
    public SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }
}