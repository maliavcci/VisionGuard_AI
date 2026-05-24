using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace VisionGuard_AI
{
    internal class Veritabani
    {
        // Veri tabanı dosyası projenin çalıştığı klasörde (Debug/Release altında) otomatik oluşacak
        private static string dbYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VisionGuard_Gecmis.db");
        private static string baglantiCumlesi => $"Data Source={dbYolu};Version=3;";

        // Proje ilk açıldığında çalışacak ve tablo yoksa oluşturacak metot
        public static void IlkKurulum()
        {
            if (!File.Exists(dbYolu))
            {
                SQLiteConnection.CreateFile(dbYolu);
                using (var baglanti = new SQLiteConnection(baglantiCumlesi))
                {
                    baglanti.Open();
                    string tabloOlustur = @"
                        CREATE TABLE IF NOT EXISTS Analizler (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Tarih TEXT,
                            Algoritma TEXT,
                            Skor TEXT
                        );";
                    using (var komut = new SQLiteCommand(tabloOlustur, baglanti))
                    {
                        komut.ExecuteNonQuery();
                    }
                }
            }
        }

        // Yapılan analizleri veri tabanına fırlatan metot
        public static void AnalizKaydet(string algoritma, string skor)
        {
            using (var baglanti = new SQLiteConnection(baglantiCumlesi))
            {
                baglanti.Open();
                string sorgu = "INSERT INTO Analizler (Tarih, Algoritma, Skor) VALUES (@tarih, @algoritma, @skor)";
                using (var komut = new SQLiteCommand(sorgu, baglanti))
                {
                    komut.Parameters.AddWithValue("@tarih", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    komut.Parameters.AddWithValue("@algoritma", algoritma);
                    komut.Parameters.AddWithValue("@skor", skor);
                    komut.ExecuteNonQuery();
                }
            }
        }

        // Geçmiş butonuna basınca verileri tabloya dolduracak metot
        public static DataTable GecmisiGetir()
        {
            DataTable dt = new DataTable();
            using (var baglanti = new SQLiteConnection(baglantiCumlesi))
            {
                baglanti.Open();
                string sorgu = "SELECT Tarih, Algoritma, Skor FROM Analizler ORDER BY ID DESC";
                using (var da = new SQLiteDataAdapter(sorgu, baglanti))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }
    }
}