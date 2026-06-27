import torch
import trade

# Подготавливаем ненормированные датасеты для нескольких акций
rosn = trade.generate_dataset('candles/ROSN-2024-2025-10min.csv', trail_size = 1024, batch_size = 1024)
moex = trade.generate_dataset('candles/MOEX-2024-2025-10min.csv', trail_size = 1024, batch_size = 1024)
sber = trade.generate_dataset('candles/SBER-2024-2025-10min.csv', trail_size = 1024, batch_size = 1024)
ydex = trade.generate_dataset('candles/YDEX-2024-2025-10min.csv', trail_size = 1024, batch_size = 1024)
gmkn = trade.generate_dataset('candles/GMKN-2024-2025-10min.csv', trail_size = 1024, batch_size = 1024)

# Объединяем несколько акций в один датасет
data = torch.cat((sber, moex, rosn, ydex, gmkn), dim = 0)

# Нормируем датасет и сохраняем отдельно нормировочные коэффициенты
parameters = torch.mean(data, dim = 2)
inputs = data / parameters.unsqueeze(2)

# Сохраняем датасет
torch.save(inputs, 'datasets/inputs.pt')
torch.save(parameters, 'datasets/parameters.pt')

# Выводим размеры датасетов
print(f"Inputs     : {inputs.size()}")
print(f"Parameters : {parameters.size()}")
