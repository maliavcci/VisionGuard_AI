# 🛡️ VisionGuard AI — Görüntü Manipülasyon Tespit Sistemi

> Yapay zeka destekli ve geleneksel bilgisayarlı görü algoritmalarını bir arada kullanan, görüntü sahteciliği tespit masaüstü uygulaması.

---

## 📌 Proje Hakkında

**VisionGuard AI**, dijital görüntülerdeki manipülasyonları ve sahte içerikleri tespit etmek amacıyla geliştirilmiş hibrit bir masaüstü uygulamasıdır. Uygulama; hem geleneksel özellik eşleştirme algoritmalarını (ORB, SIFT, AKAZE) hem de derin öğrenme tabanlı bir ResNet18 modelini aynı arayüz üzerinden kullanıcıya sunar.

Proje iki ana bileşenden oluşur:

- **WPF Masaüstü Uygulaması (C# / .NET 8)** — Görsel arayüz, analiz akışı ve rapor yönetimi
- **FastAPI Backend (Python)** — Eğitilmiş ResNet18 modelini REST API olarak sunan yapay zeka sunucusu

---

## ✨ Özellikler

- 🤖 **Yapay Zeka Modu** — Eğitilmiş ResNet18 modeli ile tek görüntü üzerinde sahtecilik skoru üretir
- 🔍 **Geleneksel Analiz Modu** — ORB, SIFT ve AKAZE algoritmaları ile iki görüntü arasında özellik eşleştirmesi yapar
- 🌡️ **Isı Haritası Görselleştirme** — Sahte tespit edildiğinde manipüle bölgeler renk haritasıyla vurgulanır
- 📊 **Eşleşme Skoru** — Her analiz için yüzde cinsinden güven skoru hesaplanır
- 🗂️ **Analiz Geçmişi** — Tüm analizler yerel veritabanına kaydedilir, geçmiş ekranından görüntülenebilir
- 📄 **Rapor İndirme** — Analiz sonuçları tarih ve algoritma bilgisiyle birlikte PNG/JPG olarak dışa aktarılır
- ⚙️ **Dinamik API Ayarları** — FastAPI sunucu adresi arayüzden değiştirilebilir

---

## 🏗️ Mimari

```
┌─────────────────────────────────┐        ┌──────────────────────────────┐
│   WPF Masaüstü Uygulaması       │        │   FastAPI Python Backend     │
│   (C# / .NET 8 / Windows)      │◄──────►│   (PyTorch / ResNet18)       │
│                                 │  HTTP  │                              │
│  • MainWindow.xaml.cs           │        │  • app.py  (REST API)        │
│  • GecmisWindow.xaml.cs         │        │  • model.py (Model Tanımı)   │
│  • YardimWindow.xaml.cs         │        │  • train.py (Eğitim)         │
│  • Veritabani.cs                │        │  • gradcam.py (Görselleştirme│
└─────────────────────────────────┘        └──────────────────────────────┘
```

---

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler

| Bileşen | Versiyon |
|---|---|
| .NET SDK | 8.0+ |
| Python | 3.9+ |
| CUDA (opsiyonel) | GPU hızlandırma için |

### 1. Python Backend Kurulumu

```bash
# Bağımlılıkları yükle
pip install fastapi uvicorn torch torchvision opencv-python numpy

# Eğitilmiş modeli kök dizine koy (model.pth)
# Sunucuyu başlat
python app.py
```

Sunucu varsayılan olarak `http://127.0.0.1:8000` adresinde çalışır.

### 2. Modeli Eğitmek (Opsiyonel)

```bash
python train.py
```

Eğitim tamamlandığında `model.pth` dosyası oluşturulur. Bu dosyayı `app.py` ile aynı dizine koyun.

### 3. WPF Uygulamasını Çalıştırma

```bash
# Projeyi derle ve çalıştır
dotnet run --project VisionGuard_AI.csproj
```

veya Visual Studio 2022'de `VisionGuard_AI.sln` dosyasını açarak F5 ile başlatın.

---

## 🎮 Kullanım

### Yapay Zeka Modu

1. Algoritma listesinden **"Yapay Zeka (CNN + LSTM)"** seçin
2. **"Analiz Edilecek Resmi Seç"** butonuna tıklayın ve görüntüyü yükleyin
3. **"Analiz Et"** butonuna basın
4. Sonuç panelinde sahtecilik skoru ve ısı haritası görüntülenir

### Geleneksel Mod (ORB / SIFT / AKAZE)

1. İstediğiniz algoritmayı seçin
2. **Orijinal** ve **Şüpheli** görüntüleri ayrı ayrı yükleyin
3. **"Analiz Et"** butonuna basın
4. Eşleşen özellik noktaları çizgi ve dairelerle gösterilir

### Rapor Alma

- Analiz tamamlandıktan sonra **"Raporu İndir"** butonu aktif hale gelir
- Rapor; tarih, algoritma adı ve skoru içeren bir başlık bandıyla PNG veya JPG olarak kaydedilir

---

## 📁 Proje Yapısı

```
VisionGuard_AI/
├── app.py                  # FastAPI REST sunucusu (AI endpoint)
├── model.py                # ResNet18 model mimarisi
├── train.py                # Model eğitim scripti
├── gradcam.py              # Grad-CAM görselleştirme
├── model.pth               # Eğitilmiş model ağırlıkları
├── dataset/                # Eğitim veri seti
├── MainWindow.xaml         # Ana pencere arayüzü
├── MainWindow.xaml.cs      # Ana pencere iş mantığı
├── GecmisWindow.xaml(.cs)  # Analiz geçmişi ekranı
├── YardimWindow.xaml(.cs)  # Yardım/kılavuz ekranı
├── Veritabani.cs           # SQLite veritabanı işlemleri
└── VisionGuard_AI.sln      # Visual Studio çözüm dosyası
```

---

## 🧠 Model Detayları

| Özellik | Değer |
|---|---|
| Mimari | ResNet18 (Transfer Learning) |
| Çıkış | 2 sınıf (Orijinal / Manipüle) |
| Girdi Boyutu | 224 × 224 piksel |
| Normalizasyon | ImageNet standartları (mean/std) |
| Donanım | CPU veya CUDA |

Yapay zeka modeli tahmin sonrasında sahtecilik bölgelerini Canny kenar tespiti ve kontur analizi ile ısı haritasına dönüştürür. Sahtecilik skoru %50 ve üzerindeyse görüntü **MANIPÜLE**, altındaysa **DOĞRULANMIŞ** olarak işaretlenir.

---

## 🔧 API Referansı

### `POST /predict`

Görüntüyü analiz eder ve işaretlenmiş görüntüyü döner.

**İstek:**
```
Content-Type: multipart/form-data
file: <görüntü dosyası>
```

**Yanıt:**
```
Content-Type: image/png
x-forgery-score: <0.00 - 100.00>
Body: İşaretlenmiş PNG görüntüsü
```

---

## 🛠️ Kullanılan Teknolojiler

**Backend (Python)**
- FastAPI & Uvicorn
- PyTorch & TorchVision
- OpenCV (cv2)
- NumPy

**Frontend (C#)**
- WPF (.NET 8, Windows)
- Emgu CV (OpenCV .NET sarmalayıcı)
- MaterialDesignThemes
- SQLite (yerel veritabanı)

---

## 📄 Lisans

Bu proje akademik ve eğitim amaçlıdır.

---

> **VisionGuard AI** — *Görüntüye güvenin, sahteyi yakala.*
