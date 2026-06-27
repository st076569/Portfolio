import trade as tr
import datetime as dt

# Скачиваем сырые торговые свечи
data = tr.get_historical_candles(start = dt.datetime(2024, 6, 1), end = dt.datetime(2025, 6, 1), ticker = 'GMKN', period = 10)

# Сохраняем свечи в файл
data.to_csv('candles/GMKN-2024-2025-10min.csv')
