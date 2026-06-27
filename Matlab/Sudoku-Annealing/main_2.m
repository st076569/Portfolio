%% Описание

% Программа решения 'Судоку' методом имитации отжига
% Баталов Семен, 2022

%% Модификаторы

% Размер субклетки поля
mod = int16(3);

% Имя файла с начальными данными
file_name = 'field_2.txt';

% Параметры имитации отжига
T_max = 0.200;  % Начальная температура
T_min = 0.030;  % Конечная температура
k_T = 9.0e-7;   % Подстройка ф-ии температуры
k_p = 1e-1;     % Подстройка ф-ии вероятности

%% Чтение входных данных, инициализация

% Размер игрового поля
N = int16(mod ^ 2);

% Начальное игровое поле
main_M = int16(readmatrix(file_name));

% Первоначальная инициализация поля и переменной ошибки
current_M = init_field(main_M, N);
current_Err = error(current_M, mod, N);
err_1 = current_Err;

%% Метод имитации отжига

% Подготовка
new_M = current_M;
new_Err = current_Err;
T = T_max;
step = 1;

% Заводим таймер
tic

% Основной цикл
while (T > T_min) && (current_Err > 0)
    
    % Формируем новую конфигурацию
    new_M = outrage(main_M, current_M, N);
    new_Err = error(new_M, mod, N);
    delta = new_Err - current_Err;
    
    % Осуществляем переход к новой конфигурации
    if is_rand_choice(transit_probability(T, delta, k_p))
        current_Err = new_Err;
        current_M = new_M;
    end
    
    % Понижаем температуру обновляем шаг
    T = temperature(T, k_T);
end

% Останавливаем таймер
toc

%% Вывод данных

fprintf('\n## Результат :\n\n');
disp(current_M);
disp([' > Начальное число коллизий : ', num2str(err_1)]);
disp([' > Конечное число коллизий  : ', num2str(current_Err)]);
disp([' > Температура (max)        : ', num2str(T_max)]);
disp([' > Температура (min)        : ', num2str(T)]);

%% Функции

% Производит инициализацию конфигурации
function C = init_field(M, N)
    C = repmat(1:1:N, N, 1);
    for i = int16(1:N)
        for j = int16(1:N)
            if (M(i, j) ~= C(i, j)) && (M(i, j) > 0)
                for k = int16(1:N)
                    if C(i, k) == M(i, j)
                        C(i, k) = C(i, j);
                    end
                end
                C(i, j) = M(i, j);
            end
        end
    end
end

% Производит случайную модификацию текущей конфигурации
function D = outrage(M, C, N)
    D = C;
    i = randi(N);
    v = randi(N, [1, 2]);
    while (M(i, v(1)) ~= 0) || (M(i, v(2)) ~= 0) || (v(1) == v(2))
        v = randi(N, [1, 2]);
    end
    temp = D(i, v(1));
    D(i, v(1)) = D(i, v(2));
    D(i, v(2)) = temp;
end

% Функция ошибки (оптимизационная функция)
function v = error(D, mod, N)
    
    % Проход по столбцам
    v = 0;
    for i = int16(1:N)
        temp = zeros(1, N);
        for j = 1:N
            temp(D(j, i)) = 1;
        end
        v = v + sum(temp == 0);
    end
    
    % Проход по субклеткам
    for i = int16(1:mod)
        for j = int16(1:mod)
            temp = zeros(1, N);
            for m = int16(1:mod)
                for n = int16(1:mod)
                    temp(D(m + mod * (i - 1), n + mod * (j - 1))) = 1;
                end
            end
            v = v + sum(temp == 0);
        end
    end
end

% Функция понижения температуры
function T = temperature(T_prev, k)
    T = T_prev * exp(-k);
end

% Реализует выбор с некоторой вероятностью
function v = is_rand_choice(p)
    if rand() < p
        v = 1;
    else
        v = 0;
    end
end

% Возвращает вероятность принятия решения
function p = transit_probability(T, delta, k)
    if delta <= 0
        p = 1;
    else
        p = exp(-k * delta / T);
        if p > 1
            p = 1;
        end
    end
end