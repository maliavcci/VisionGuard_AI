using System;
using System.Data;
using System.Windows;

namespace VisionGuard_AI
{
    public partial class GecmisWindow : Window
    {
        public GecmisWindow()
        {
            InitializeComponent();

            try
            {
                // Veri tabanından geçmiş verileri çekip DataGrid'e basıyoruz
                DataTable dt = Veritabani.GecmisiGetir();
                dgGecmis.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geçmiş veriler yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}