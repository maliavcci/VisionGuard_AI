using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using System.Windows.Controls;
using Emgu.CV.CvEnum;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

// 🔥 ÇAKIŞMA ÇÖZÜMÜ
using DrawingPoint = System.Drawing.Point;
using DrawingSize = System.Drawing.Size;
using DrawingRectangle = System.Drawing.Rectangle;
using System.Drawing;

namespace VisionGuard_AI
{
    public partial class MainWindow : Window
    {
        private Mat matOrijinal = null;
        private Mat matSupheli = null;

        // 🌟 DİNAMİK API URL YÖNETİMİ (Ayarlar butonundan kontrol edilebilir)
        public static string ApiUrl = "http://127.0.0.1:8000/predict";

        public MainWindow()
        {
            InitializeComponent();
        }

        // 🔥 Algoritma seçildiğinde tetiklenen dinamik arayüz gizleme mekanizması
        private void comboAlgoritma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colOrijinal == null || colSupheli == null || txtSupheliLabel == null) return;

            var selectedItem = comboAlgoritma.SelectedItem as ComboBoxItem;
            string secilen = selectedItem?.Content.ToString() ?? "";

            if (secilen.Contains("Yapay Zeka"))
            {
                colOrijinal.Width = new GridLength(0);
                txtSupheliLabel.Text = "Analiz Edilecek Resmi Seç (Yapay Zeka Modu)";
            }
            else
            {
                colOrijinal.Width = new GridLength(1, GridUnitType.Star);
                colSupheli.Width = new GridLength(1, GridUnitType.Star);
                txtSupheliLabel.Text = "Şüpheli Resmi Seç";
            }
        }

        private void btnOrijinalYukle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            matOrijinal = ResimSecVeYukle(imgOrijinal, stackOrijinalHint);
            if (matOrijinal != null) MainSnackbar.MessageQueue.Enqueue("Orijinal resim yüklendi.");
        }

        private void btnSupheliYukle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            matSupheli = ResimSecVeYukle(imgSupheli, stackSupheliHint);
            if (matSupheli != null) MainSnackbar.MessageQueue.Enqueue("Analiz edilecek şüpheli resim yüklendi.");
        }

        private Mat ResimSecVeYukle(System.Windows.Controls.Image displayImage, StackPanel hintPanel)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resim Dosyaları (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    Mat mat = new Mat();
                    CvInvoke.Imdecode(imageBytes, ImreadModes.Color, mat);

                    if (!mat.IsEmpty)
                    {
                        displayImage.Source = BitmapSourceConvert(mat);
                        hintPanel.Visibility = Visibility.Collapsed;
                        return mat;
                    }
                }
                catch (Exception ex)
                {
                    MainSnackbar.MessageQueue.Enqueue("Resim yükleme hatası: " + ex.Message);
                }
            }
            return null;
        }

        // 🔥 ASENKRON ÇİFTLİ ANALİZ BUTONU (Geleneksel & Derin Öğrenme)
        private async void btnAnalizEt_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = comboAlgoritma.SelectedItem as ComboBoxItem;
            string secilen = selectedItem?.Content.ToString() ?? "ORB";

            // 🌟 1. YOL: YAPAY ZEKA MODU (Sadece tek resim yeterli!)
            if (secilen.Contains("Yapay Zeka"))
            {
                if (matSupheli == null)
                {
                    MainSnackbar.MessageQueue.Enqueue("Lütfen analiz edilecek resmi yükleyin!");
                    return;
                }
                await YapayZekaAnaliziBaslat();
                return;
            }

            // 🌟 2. YOL: GELENEKSEL MOD (İki resim birden zorunlu)
            if (matOrijinal == null || matSupheli == null)
            {
                MainSnackbar.MessageQueue.Enqueue("Geleneksel algoritmalar için lütfen her iki resmi de yükleyin!");
                return;
            }

            try
            {
                using (Mat griOrijinal = new Mat())
                using (Mat griSupheli = new Mat())
                {
                    CvInvoke.CvtColor(matOrijinal, griOrijinal, ColorConversion.Bgr2Gray);
                    CvInvoke.CvtColor(matSupheli, griSupheli, ColorConversion.Bgr2Gray);
                    CvInvoke.Resize(griSupheli, griSupheli, griOrijinal.Size);

                    VectorOfKeyPoint kpOrijinal = new VectorOfKeyPoint();
                    VectorOfKeyPoint kpSupheli = new VectorOfKeyPoint();
                    Mat descOrijinal = new Mat();
                    Mat descSupheli = new Mat();

                    using (Feature2D algoritmaInstance = GetAlgorithm(secilen))
                    {
                        algoritmaInstance.DetectAndCompute(griOrijinal, null, kpOrijinal, descOrijinal, false);
                        algoritmaInstance.DetectAndCompute(griSupheli, null, kpSupheli, descSupheli, false);
                    }

                    if (kpOrijinal.Size == 0 || kpSupheli.Size == 0 || descOrijinal.IsEmpty || descSupheli.IsEmpty)
                    {
                        MainSnackbar.MessageQueue.Enqueue("Analiz için yeterli özellik noktası bulunamadı.");
                        return;
                    }

                    DistanceType dType = secilen.Contains("ORB") ? DistanceType.Hamming : DistanceType.L2;

                    using (BFMatcher matcher = new BFMatcher(dType))
                    using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
                    {
                        matcher.KnnMatch(descOrijinal, descSupheli, matches, 2);

                        using (VectorOfDMatch goodMatches = new VectorOfDMatch())
                        {
                            for (int i = 0; i < matches.Size; i++)
                            {
                                if (matches[i].Size < 2) continue;
                                var m1 = matches[i][0];
                                var m2 = matches[i][1];

                                if (m1.Distance < 0.80 * m2.Distance)
                                {
                                    if (m1.QueryIdx >= 0 && m1.QueryIdx < kpOrijinal.Size &&
                                        m1.TrainIdx >= 0 && m1.TrainIdx < kpSupheli.Size)
                                    {
                                        goodMatches.Push(new MDMatch[] { m1 });
                                    }
                                }
                            }

                            if (goodMatches.Size == 0)
                            {
                                MainSnackbar.MessageQueue.Enqueue("Geçerli eşleşme bulunamadı.");
                                return;
                            }

                            int width = matOrijinal.Width + matSupheli.Width;
                            int height = Math.Max(matOrijinal.Height, matSupheli.Height);

                            using (Mat cikisMat = new Mat(new DrawingSize(width, height), DepthType.Cv8U, 3))
                            {
                                cikisMat.SetTo(new MCvScalar(0, 0, 0));
                                matOrijinal.CopyTo(new Mat(cikisMat, new DrawingRectangle(0, 0, matOrijinal.Width, matOrijinal.Height)));
                                matSupheli.CopyTo(new Mat(cikisMat, new DrawingRectangle(matOrijinal.Width, 0, matSupheli.Width, matSupheli.Height)));

                                for (int i = 0; i < goodMatches.Size; i++)
                                {
                                    var match = goodMatches[i];
                                    var pt1 = kpOrijinal[match.QueryIdx].Point;
                                    var pt2 = kpSupheli[match.TrainIdx].Point;
                                    pt2.X += matOrijinal.Width;

                                    CvInvoke.Line(cikisMat, DrawingPoint.Round(pt1), DrawingPoint.Round(pt2), new MCvScalar(0, 255, 0), 1);
                                    CvInvoke.Circle(cikisMat, DrawingPoint.Round(pt1), 3, new MCvScalar(255, 0, 0), -1);
                                    CvInvoke.Circle(cikisMat, DrawingPoint.Round(pt2), 3, new MCvScalar(0, 0, 255), -1);
                                }

                                imgSonuc.Source = BitmapSourceConvert(cikisMat);
                                txtSonucHint.Visibility = Visibility.Collapsed;

                                double skor = Math.Min(100, (double)goodMatches.Size / Math.Min(kpOrijinal.Size, kpSupheli.Size) * 100);
                                prgAnaliz.Value = skor;
                                txtSkor.Text = $"Eşleşme Oranı: %{skor:F1} (Eşleşme: {goodMatches.Size})";

                                // 🌟 VERİ TABANI KAYDI: Geleneksel analiz sonucunu kaydeder
                                Veritabani.AnalizKaydet(secilen, $"%{skor:F1}");

                                btnRaporIndir.IsEnabled = true;
                                MainSnackbar.MessageQueue.Enqueue("Analiz Başarılı! Rapor oluşturuldu.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainSnackbar.MessageQueue.Enqueue("Hata: " + ex.Message);
            }
        }

        // 🔥 HTTP CLIENT İLE FASTAPI BAĞLANTI METODU (CNN + LSTM / ResNet18)
        private async Task YapayZekaAnaliziBaslat()
        {
            MainSnackbar.MessageQueue.Enqueue("Yapay Zeka Modeli Çağrılıyor...");
            prgAnaliz.Value = 15;

            try
            {
                byte[] supheliBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapSource)imgSupheli.Source));
                    encoder.Save(ms);
                    supheliBytes = ms.ToArray();
                }

                prgAnaliz.Value = 40;

                using (var client = new HttpClient())
                {
                    // 🌟 AYARLAR PANELİNDEN GELEN DİNAMİK URL KULLANILIYOR
                    string apiUri = MainWindow.ApiUrl;

                    using (var content = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(supheliBytes);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        content.Add(fileContent, "file", "supheli.png");

                        prgAnaliz.Value = 60;

                        HttpResponseMessage response = await client.PostAsync(apiUri, content);

                        if (response.IsSuccessStatusCode)
                        {
                            prgAnaliz.Value = 85;

                            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();

                            double aiSkor = 0.0;
                            if (response.Headers.Contains("x-forgery-score"))
                            {
                                var scoreHeader = response.Headers.GetValues("x-forgery-score");
                                foreach (var value in scoreHeader)
                                {
                                    double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out aiSkor);
                                }
                            }

                            Mat aiSonucMat = new Mat();
                            CvInvoke.Imdecode(responseBytes, ImreadModes.Color, aiSonucMat);

                            imgSonuc.Source = BitmapSourceConvert(aiSonucMat);
                            txtSonucHint.Visibility = Visibility.Collapsed;

                            prgAnaliz.Value = 100;
                            txtSkor.Text = $"AI Sahtecilik Skoru: %{aiSkor:F1}";

                            // 🌟 VERİ TABANI KAYDI: Yapay zeka analiz sonucunu kaydeder
                            Veritabani.AnalizKaydet("CNN + LSTM (Yapay Zeka)", $"%{aiSkor:F1}");

                            btnRaporIndir.IsEnabled = true;
                            MainSnackbar.MessageQueue.Enqueue("Yapay Zeka Analizi Başarıyla Tamamlandı!");
                        }
                        else
                        {
                            MainSnackbar.MessageQueue.Enqueue("AI Sunucusu hata kodu döndü.");
                            prgAnaliz.Value = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainSnackbar.MessageQueue.Enqueue("AI Sunucu Bağlantı Hatası: " + ex.Message);
                prgAnaliz.Value = 0;
            }
        }

        // 🌟 SOL MENÜ: GEÇMİŞ BUTONU AKSİYONU
        private void btnGecmis_Click(object sender, RoutedEventArgs e)
        {
            GecmisWindow gecmisEkran = new GecmisWindow();
            gecmisEkran.Owner = this;
            gecmisEkran.ShowDialog();
        }

        // 🌟 SOL MENÜ: AYARLAR BUTONU AKSİYONU (Dinamik Port / IP Değişimi için Sunum Kalesi)
        private void btnAyarlar_Click(object sender, RoutedEventArgs e)
        {
            string yeniUrl = Microsoft.VisualBasic.Interaction.InputBox(
                "Lütfen FastAPI Sunucu Adresini Giriniz:", 
                "VisionGuard - Gelişmiş API Ayarları", 
                MainWindow.ApiUrl);

            if (!string.IsNullOrWhiteSpace(yeniUrl))
            {
                MainWindow.ApiUrl = yeniUrl;
                MainSnackbar.MessageQueue.Enqueue($"API Endpoint başarıyla güncellendi: {MainWindow.ApiUrl}");
            }
        }

        // 🌟 SOL MENÜ: YARDIM BUTONU AKSİYONU (Kullanım Klavuzu / Akademik Prosedür)
        // 🌟 SOL MENÜ: YARDIM BUTONU AKSİYONU (Yeni Modern Pencere)
        private void btnYardim_Click(object sender, RoutedEventArgs e)
        {
            YardimWindow yardimEkran = new YardimWindow();
            yardimEkran.Owner = this; // Ana ekranın tam ortasında açılması için
            yardimEkran.ShowDialog();
        }

        private void btnRaporIndir_Click(object sender, RoutedEventArgs e)
        {
            if (imgSonuc.Source == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = $"VisionGuard_Rapor_{DateTime.Now:yyyyMMdd_HHmm}";
            saveDialog.Filter = "PNG Dosyası (*.png)|*.png|JPEG Dosyası (*.jpg)|*.jpg";

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmapSource = (BitmapSource)imgSonuc.Source;
                    using (Mat sonucMat = BitmapSourceToMat(bitmapSource))
                    {
                        int ekSertYukseklik = (int)(sonucMat.Height * 0.1);
                        if (ekSertYukseklik < 65) ekSertYukseklik = 65;

                        using (Mat raporMat = new Mat(new DrawingSize(sonucMat.Width, sonucMat.Height + ekSertYukseklik), sonucMat.Depth, sonucMat.NumberOfChannels))
                        {
                            raporMat.SetTo(new MCvScalar(30, 30, 30));
                            sonucMat.CopyTo(new Mat(raporMat, new DrawingRectangle(0, 0, sonucMat.Width, sonucMat.Height)));

                            var selectedItem = comboAlgoritma.SelectedItem as ComboBoxItem;
                            string algoritma = selectedItem?.Content.ToString() ?? "Bilinmiyor";
                            string skor = txtSkor.Text;
                            string tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                            string raporMetni = $"[VISION GUARD AI] | Tarih: {tarih} | Algoritma: {algoritma} | {skor}";

                            double fontOlcegi = raporMat.Width / 1100.0;
                            int fontKalinligi = (int)Math.Max(1, fontOlcegi * 2);

                            CvInvoke.PutText(raporMat, raporMetni, new DrawingPoint(25, raporMat.Height - (ekSertYukseklik / 3)),
                                FontFace.HersheySimplex, fontOlcegi, new MCvScalar(255, 255, 255), fontKalinligi);

                            raporMat.Save(saveDialog.FileName);
                        }
                    }
                    MainSnackbar.MessageQueue.Enqueue("Detaylı rapor başarıyla kaydedildi!");
                }
                catch (Exception ex)
                {
                    MainSnackbar.MessageQueue.Enqueue("Rapor hatası: " + ex.Message);
                }
            }
        }

        private Mat BitmapSourceToMat(BitmapSource source)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(ms);
                using (Bitmap bmp = new Bitmap(ms))
                {
                    Image<Bgr, byte> emguImage = bmp.ToImage<Bgr, byte>();
                    return emguImage.Mat;
                }
            }
        }

        private Feature2D GetAlgorithm(string name)
        {
            if (name.Contains("SIFT")) return new SIFT();
            if (name.Contains("AKAZE")) return new AKAZE();
            if (name.Contains("ORB")) return new ORBDetector();
            return new SIFT();
        }

        public static BitmapSource BitmapSourceConvert(Mat mat)
        {
            using (Bitmap bitmap = mat.ToBitmap())
            {
                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally { DeleteObject(hBitmap); }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}