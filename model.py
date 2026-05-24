import torch
import torch.nn as nn
import torchvision.models as models

class CNN_LSTM_Model(nn.Module):
    def __init__(self):
        super().__init__()
        
        resnet = models.resnet18(pretrained=True)
        self.cnn = nn.Sequential(*list(resnet.children())[:-2])  # feature extractor
        
        self.lstm = nn.LSTM(input_size=512, hidden_size=128, batch_first=True)
        
        self.fc = nn.Linear(128, 1)
        self.sigmoid = nn.Sigmoid()

    def forward(self, x):
        b, c, h, w = x.size()
        
        features = self.cnn(x)  # [b, 512, 7, 7]
        
        features = features.permute(0, 2, 3, 1).contiguous()
        features = features.view(b, -1, 512)  # [b, 49, 512]
        
        lstm_out, _ = self.lstm(features)
        
        out = self.fc(lstm_out[:, -1, :])
        
        return self.sigmoid(out)