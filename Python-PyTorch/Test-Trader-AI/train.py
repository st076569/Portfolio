import matplotlib.pyplot as plt
import trade
import torch
import random
from torch.utils import data
from torch import nn, optim
from torchmetrics import regression as reg
from torch.utils.data import DataLoader, TensorDataset

# Создаем новую модель или читаем ее из файла
model = trade.Conv1DNetSequential(multiplier = 0.1)
model.load_state_dict(torch.load('models/conv1d-5x1024-b128.pth'))

# Определяем функцию потерь и оптимизатор
criterion = trade.ProfitLossSequential(0.04, start_currency = 0.99)
optimizer = optim.Adam(model.parameters(), lr = 1e-6)

# Создаем DataLoader
dataset = TensorDataset(torch.load('datasets/inputs.pt'), torch.load('datasets/parameters.pt'))
dataloader = DataLoader(dataset, batch_sampler = trade.SequentialBatchLoader(dataset, batch_size = 128, shuffle = True))

# Запускаем обучение
history = trade.train_model(model, dataloader, criterion, optimizer, num_epochs = 5, device = 'cuda')

# Сохраняем модель в файл
#torch.save(model.state_dict(), 'models/conv1d-5x1024-b128.pth')

plt.figure(1)
plt.hist(history, 50)
#plt.figure(2)
#plt.hist(output.flatten(), 100)
plt.show()

"""
running_loss = 1.0

for input, param in dataloader:
    output = 0.1 * torch.empty(128).uniform_(-1.0, 1.0).to('cuda').requires_grad_(True)
    #output = -0.1 * torch.ones(128).to('cuda').requires_grad_(True)

    input = input.to('cuda')
    param = param.to('cuda')

    loss = criterion(output, param[:, 1], input[:, 1, 0])
    loss.backward()
    # running_loss -= loss.item()
    running_loss *= 1.0 - loss.item() / 100.0
    print(f"# {-loss.item():.4f}")

#epoch_loss = running_loss / len(dataloader)
epoch_loss = (running_loss ** (1.0 / len(dataloader)) - 1.0) * 100.0

print(epoch_loss)
print(len(dataloader))
"""
