# VisionGuard AI

> A hybrid image-forensics desktop application that combines deep learning with traditional computer-vision algorithms.

![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91)
![Python](https://img.shields.io/badge/Python-3.9%2B-3776AB?logo=python&logoColor=white)
![FastAPI](https://img.shields.io/badge/API-FastAPI-009688?logo=fastapi&logoColor=white)
![PyTorch](https://img.shields.io/badge/ML-PyTorch-EE4C2C?logo=pytorch&logoColor=white)

## Overview

VisionGuard AI detects possible image manipulation through two complementary workflows:

- **AI analysis:** a FastAPI service runs a trained ResNet18 classifier and returns a forgery score together with an annotated image.
- **Feature matching:** the WPF client compares an original and a suspicious image using ORB, SIFT or AKAZE.

The desktop client keeps a local analysis history in SQLite and can export the generated result as an image report.

## Features

- ResNet18-based original/manipulated image classification
- ORB, SIFT and AKAZE feature matching through Emgu CV
- Configurable FastAPI endpoint
- Forgery or similarity score presentation
- Annotated image and heat-map-style result output
- Local analysis history with SQLite
- PNG/JPG report export

## Architecture

```text
WPF desktop client (.NET 8)
        |
        | multipart/form-data / HTTP
        v
FastAPI inference service (Python)
        |
        v
ResNet18 model + OpenCV result rendering
```

Traditional ORB/SIFT/AKAZE comparisons run locally in the WPF application. AI inference is performed by the Python service through `POST /predict`.

## Tech Stack

| Layer | Technologies |
|---|---|
| Desktop UI | C#, .NET 8, WPF, Material Design |
| Computer vision | Emgu CV / OpenCV, ORB, SIFT, AKAZE |
| AI service | Python, FastAPI, Uvicorn |
| Machine learning | PyTorch, TorchVision, ResNet18 |
| Local storage | SQLite |

## Getting Started

### Prerequisites

- Windows
- .NET 8 SDK
- Python 3.9+
- Visual Studio 2022 (recommended for the WPF client)
- CUDA-compatible GPU is optional

### 1. Clone the repository

```bash
git clone https://github.com/omer-damar/VisionGuard_AI.git
cd VisionGuard_AI
```

### 2. Start the AI service

Create and activate a virtual environment, then install the Python dependencies:

```bash
python -m venv .venv
```

Windows PowerShell:

```powershell
.\.venv\Scripts\Activate.ps1
pip install fastapi uvicorn python-multipart torch torchvision opencv-python numpy
python app.py
```

The API starts at `http://127.0.0.1:8000`. The included `model.pth` file is loaded from the repository root.

### 3. Run the desktop client

In another terminal:

```bash
dotnet restore
dotnet run --project VisionGuard_AI.csproj
```

You can also open `VisionGuard_AI.sln` in Visual Studio and run the project with `F5`.

## API

### `POST /predict`

Accepts an image as `multipart/form-data` under the `file` field.

The response body is an annotated PNG image. The prediction score is returned in the `x-forgery-score` response header.

```bash
curl -X POST http://127.0.0.1:8000/predict \
  -F "file=@sample.jpg" \
  --output analyzed.png \
  -D headers.txt
```

## Model Training

The repository contains a two-class `ImageFolder` dataset layout and a training script that fine-tunes ResNet18:

```text
dataset/
├── fake/
└── real/
```

To retrain the model:

```bash
python train.py
```

The generated weights are saved as `model.pth`.

## Project Structure

```text
VisionGuard_AI/
├── app.py                   # FastAPI inference endpoint
├── train.py                 # ResNet18 training script
├── model.py                 # Experimental CNN-LSTM model definition
├── gradcam.py               # Grad-CAM helper
├── model.pth                # Trained model weights
├── dataset/                 # Training images
├── MainWindow.xaml(.cs)     # Main WPF interface and analysis flow
├── GecmisWindow.xaml(.cs)   # Analysis history
├── YardimWindow.xaml(.cs)   # Help screen
├── Veritabani.cs            # SQLite operations
└── VisionGuard_AI.csproj    # .NET project file
```

## Notes

- This is an academic/educational project, not a production forensic system.
- A high score is a model prediction and should not be treated as conclusive forensic evidence.
- The current annotated region is produced with OpenCV edge/contour processing after classification; it is not a pixel-level segmentation mask.

