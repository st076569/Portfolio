from torch import nn
import tqdm
import torch
import random

from torch.utils.data import Dataset
from moexalgo import Ticker
import pandas as pd
import datetime as dt

# Процедура загрузки исторических свечей
def get_historical_candles(start = dt.datetime(2025, 4, 1), end = dt.datetime(2025, 5, 1), ticker = 'MOEX', period = 1):

    # Устанавливаем инструмент
    stock = Ticker(ticker)

    # Скачиваем по 10 дней (10000 запросов - лимит)
    delta = dt.timedelta(days = 10)

    # Инициализация
    current_start = start
    full_data = pd.DataFrame()

    # Скачиваем свечи по частям (по 10 дней)
    while current_start < end:
        current_end = min(current_start + delta, end)
        print(f"Загрузка свечей с {current_start} по {current_end}")
        candles = stock.candles(start = current_start, end = current_end, period = period)
        full_data = pd.concat([full_data, candles], axis = 0, ignore_index = True)
        current_start = current_end
    return full_data

def generate_dataset(path, trail_size = 1024, batch_size = 1024):

    # Извлекаем необходимые свойства свечей
    data = pd.read_csv(path, index_col = 0)
    tensor = torch.tensor(data[['open', 'close', 'high', 'low', 'volume']].values, dtype = torch.float32)

    # Формируем ненормированный датасет
    dataset = tensor.flip(dims = [0]).unfold(0, trail_size, 1)
    N = (dataset.size(0) // batch_size) * batch_size

    # Возвращаем датасет (от старого к новому)
    return dataset[:N].flip(dims = [0])

# Генератор последовательных батчей (возможен случайный порядок следования батчей)
class SequentialBatchLoader:
    def __init__(self, dataset, batch_size = 1024, shuffle = True):
        self.dataset = dataset
        self.batch_size = batch_size
        self.shuffle = shuffle
        self.indices = list(range(0, len(self.dataset) - self.batch_size, self.batch_size))

    def __iter__(self):

        # Перемешиваем порядок батчей
        if self.shuffle:
            random.shuffle(self.indices)

        # Генерируем последовательность случайных батчей
        for start in self.indices:
            end = start + self.batch_size
            yield list(range(start, end))

    def __len__(self):
        return len(self.indices)

# Классическая аддитивная нейросеть с несколькими внутренними слоями
class PerceptronTrader(nn.Module):
    def __init__(self, input_size = 1024, hidden_size = 256, drop = 0.1, leak = 0.01):
        super().__init__()
        self.net = nn.Sequential(
            nn.Linear(input_size, hidden_size),
            nn.LeakyReLU(leak),
            nn.Dropout(drop),
            nn.Linear(hidden_size, hidden_size),
            nn.LeakyReLU(leak),
            nn.Dropout(drop),
            nn.Linear(hidden_size, hidden_size),
            nn.LeakyReLU(leak),
            nn.Dropout(drop),
            nn.Linear(hidden_size, hidden_size),
            nn.LeakyReLU(leak),
            nn.Dropout(drop),
            nn.Linear(hidden_size, 1),
            nn.Tanh()
        )
    def forward(self, x):
        return self.net(x)

# Сверточная нейронная сеть для анализа входных данных
class Conv1DNetSequential(nn.Module):
    def __init__(self, input_channels = 5, input_size = 1024, hidden_size = 256, leak = 0.01, drop = 0.5, multiplier = 0.1):
        super().__init__()
        self.flatten = nn.Flatten()
        self.multiplier = multiplier

        # Основная последовательность слоев
        self.features = nn.Sequential(
            # Блок 1
            nn.Conv1d(input_channels, 32, kernel_size = 7, padding = 3, stride = 2),
            nn.BatchNorm1d(32),
            nn.LeakyReLU(leak),
            nn.MaxPool1d(kernel_size = 2, stride = 2),

            # Блок 2
            nn.Conv1d(32, 64, kernel_size = 5, padding = 2, stride = 2),
            nn.BatchNorm1d(64),
            nn.LeakyReLU(leak),
            nn.MaxPool1d(kernel_size = 2, stride = 2),

            # Блок 3
            nn.Conv1d(64, 128, kernel_size = 3, padding = 1, stride = 1),
            nn.BatchNorm1d(128),
            nn.LeakyReLU(leak),
            nn.MaxPool1d(kernel_size = 2, stride = 2),
        )

        # Вычисление размера выхода сверточных слоев
        with torch.no_grad():
            dummy_input = torch.zeros(1, input_channels, input_size)
            dummy_output = self.features(dummy_input)
            fc_input_size = self.flatten(dummy_output).shape[1]

        # Полносвязные слои
        self.classifier = nn.Sequential(
            nn.Linear(fc_input_size, hidden_size),
            nn.LeakyReLU(leak),
            nn.Dropout(drop),
            nn.Linear(hidden_size, 1),
            nn.Tanh()
        )

    def forward(self, x):
        x = self.features(x)
        x = self.flatten(x)
        x = self.multiplier * self.classifier(x)
        return x

# Критерий, возвращающий прибыль за батч как долю первоначального вклада
class ProfitLoss(nn.Module):
    def __init__(self, comission = 0.04):
        super().__init__()
        self.comission = comission

    # outputs[batch_size], params[batch_size], inputs[batch_size]
    def forward(self, outputs, params, inputs):

        # Учитываем комиссию брокера, начальные данные: currency = 1.0, ticker = 0.0
        # outputs > 0 |=> продаем акции и покупаем рубли, если outputs < 0, то наоборот
        sum_ticker = -torch.sum(outputs / inputs / params) * inputs[-1] * params[-1]
        sum_currency = 1.0 + torch.sum(outputs) - self.comission / 100.0 * torch.sum(torch.abs(outputs))

        # Рассчитываем изменение капитала в процентах
        profit = (sum_currency + sum_ticker - 1.0) * 100.0
        return -profit

# Критерий, возвращающий прибыль за батч как долю первоначального вклада
class ProfitLossSequential(nn.Module):
    def __init__(self, comission = 0.04, limit_currency = 1.0, start_currency = 0.5):
        super().__init__()
        self.comission = comission / 100.0
        self.limit_currency = limit_currency
        self.start_currency = start_currency

    # outputs[batch_size], params[batch_size], inputs[batch_size]
    def forward(self, outputs, params, inputs):

        alpha_ticker = inputs * params
        start_ticker = (self.limit_currency - self.start_currency) / alpha_ticker[0]
        h = torch.where(outputs > 0.0, 1.0, 0.0)

        # Расчет эволюции валютного депозита
        a_currency = (1.0 - self.comission) * self.limit_currency * h * outputs
        b_currency = 1.0 + (1.0 - (2.0 - self.comission) * h) * outputs
        c_currency = torch.cumprod(b_currency, dim = 0)
        currency = c_currency * (torch.cumsum(a_currency / c_currency, dim = 0) + self.start_currency)

        # Расчет эволюции депозита акций
        a_ticker = outputs / alpha_ticker * (currency * (2.0 * h - 1.0 + self.comission * (1.0 - h)) - self.limit_currency * h)
        ticker = torch.cumsum(a_ticker, dim = 0) + start_ticker

        #print(f"{currency[0]:.4f}, {ticker[0]:.4f}")
        #print(f"{currency[-1]:.4f}, {ticker[-1]:.4f}")

        # Рассчитываем изменение капитала в процентах
        profit = (currency[-1] + ticker[-1] * alpha_ticker[-1] - self.limit_currency) * 100.0
        return -profit

# Процедура обучения
def train_model(model, dataloader, criterion, optimizer, num_epochs = 10, device = None):

    # Определяем устройство (GPU/CPU)
    if device is None:
        device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model.to(device)

    history = torch.zeros(len(dataloader))
    #output = torch.zeros(len(dataloader), 256)

    # Цикл обучения
    for epoch in tqdm.tqdm(range(num_epochs), desc = 'Training model'):

        # Переводим модель в режим обучения
        model.train()
        running_profit = 1.0
        i = 0

        for inputs, params in dataloader:

            # Перенос данных на выбранное устройство
            inputs = inputs.to(device)
            params = params.to(device)

            # Обнуляем градиенты
            optimizer.zero_grad()

            # Прямой проход
            outputs = model(inputs)
            loss = criterion(outputs.squeeze(), params[:, 1], inputs[:, 1, 0])

            # Обратный проход и оптимизация
            loss.backward()
            optimizer.step()

            if (loss.item() < 100.0):
                running_profit *= 1.0 - loss.item() / 100.0
            else:
                print(f"Out of range: {-loss.item()}")

            history[i] = -loss.item()
            #output[i] = outputs.squeeze().detach()
            i += 1
            #running_profit -= loss.item()

        # Сохраняем среднюю ошибку за эпоху
        epoch_profit = (running_profit ** (1.0 / len(dataloader)) - 1.0) * 100.0
        #epoch_profit = running_profit / len(dataloader)

        # Выводим статистику
        if (epoch + 1) % 1 == 0:
            print(f"\nEpoch {epoch + 1}/{num_epochs}, Profit: {epoch_profit:.4f} %, Len: {len(dataloader)}")

    print('Training is finished!')
    return history
