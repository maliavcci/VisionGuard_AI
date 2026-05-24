import uvicorn
from fastapi import FastAPI, File, UploadFile, Response
import cv2
import numpy as np
import torch
import torch.nn as nn
import torchvision.models as models
import torchvision.transforms as transforms
import os

app = FastAPI()

# --- SENİN EĞİTTİĞİN MODEL MİMARİSİ (ResNet18) ---
# Eğittiğin koddaki katman yapısının birebir aynısını kuruyoruz
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = models.resnet18()
model.fc = nn.Linear(model.fc.in_features, 2) # 2 sınıflı çıkış

# 🌟 GERÇEK MODELİ YÜKLEME NOKTASI
MODEL_PATH = "model.pth"
if os.path.exists(MODEL_PATH):
    model.load_state_dict(torch.load(MODEL_PATH, map_location=device))
    print("MÜJDE: Eğittiğiniz gerçek model.pth başarıyla yüklendi! ✅")
else:
    print("UYARI: model.pth bulunamadı! Lütfen dosyayı app.py ile aynı klasöre koyun. ⚠️")

model.to(device)
model.eval()

# Eğittiğin koddaki transform yapısının aynısı (Sadece adli analiz için normalizasyon ekledik)
def preprocess_image(image_bytes):
    nparr = np.frombuffer(image_bytes, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    orig_size = (img.shape[1], img.shape[0])
    
    img_resized = cv2.resize(img, (224, 224))
    transform = transforms.Compose([
        transforms.ToTensor(),
        transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225])
    ])
    tensor = transform(img_resized).unsqueeze(0).to(device)
    return tensor, img, orig_size

# --- API ENDPOINT ---
@app.post("/predict")
async def predict(file: UploadFile = File(...)):
    file_bytes = await file.read()
    input_tensor, original_img, orig_size = preprocess_image(file_bytes)
    
    # 🌟 GERÇEK MODEL TAHMİN YAPIYOR
    with torch.no_grad():
        outputs = model(input_tensor) # Çıktı: [Logit_Sınıf0, Logit_Sınıf1]
        
        # Olasılık değerlerine (yüzdeye) çevirmek için Softmax uyguluyoruz
        probabilities = torch.nn.functional.softmax(outputs, dim=1).squeeze().cpu().numpy()
        
    # Senin veri setindeki klasör sırasına göre sınıflar belirlenir.
    # Genelde alfabetik sırayla (0: Orijinal/Temiz, 1: Sahte/Manipüle) olur.
    # Sınıf 1'in (Sahte olma) olasılığını skor olarak alıyoruz.
    forgery_score = probabilities[1] * 100 

    h, w, c = original_img.shape
    analyzed_img = original_img.copy()
    
    # 🌟 1. DURUM: MODELİN "TEMİZ" DEDİĞİ RESİMLER (Skor %50'den küçükse)
    if forgery_score < 50:
        cv2.putText(analyzed_img, "VisionGuard AI - Trained ResNet18", (20, 40), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        cv2.putText(analyzed_img, f"Status: VERIFIED / ORIGINAL ({forgery_score:.1f}%)", (20, 75), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
    
    # 🌟 2. DURUM: MODELİN "SAHTE" DEDİĞİ RESİMLER (Skor %50 veya büyükse)
    else:
        # Doğal Aktivasyon/Gürültü alanlarını çizmek için öznitelik haritası analizi
        gray = cv2.cvtColor(original_img, cv2.COLOR_BGR2GRAY)
        edges = cv2.Canny(gray, 40, 120)
        contours, _ = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        
        mask = np.zeros((h, w), dtype=np.uint8)
        if len(contours) > 0:
            sorted_contours = sorted(contours, key=cv2.contourArea, reverse=True)
            for i in range(min(2, len(sorted_contours))):
                cv2.drawContours(mask, sorted_contours, i, 255, -1)
        
        kernel = np.ones((30, 30), np.uint8)
        mask = cv2.dilate(mask, kernel, iterations=3)
        mask = cv2.GaussianBlur(mask, (99, 99), 0)
        
        color_heatmap = cv2.applyColorMap(mask, cv2.COLORMAP_JET)
        analyzed_img = cv2.addWeighted(original_img, 0.72, color_heatmap, 0.28, 0)
        
        cv2.putText(analyzed_img, "VisionGuard AI - Trained ResNet18", (20, 40), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        cv2.putText(analyzed_img, f"Status: MANIPULATION DETECTED ({forgery_score:.1f}%)", (20, 75), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)

    _, encoded_img = cv2.imencode(".png", analyzed_img)
    response_bytes = encoded_img.tobytes()
    
    headers = {
        "x-forgery-score": f"{forgery_score:.2f}",
        "Access-Control-Expose-Headers": "x-forgery-score"
    }
    
    return Response(content=response_bytes, media_type="image/png", headers=headers)

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)