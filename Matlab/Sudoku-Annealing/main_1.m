%% Описание

% Программа решения 'Судоку' методом имитации отжига
% Баталов Семен, 2022

%% Модификаторы

% Размер субклетки поля
mod = int16(3);

% Имя файла с начальными данными
file_name = 'field_1.txt';

% Параметры имитации отжига
T_0 = 1e1;      % начальная температура
k_T = 1.5e-5;     % подстройка ф-ии температуры
k_p = 1e-3;     % подстройка ф-ии вероятности
step_N = 1e6;   % кол-во итераций
num = 3;

%% Чтение входных данных, инициализация

% Размер игрового поля
N = int16(mod ^ 2);

% Начальное игровое поле
main_M = int16(readmatrix(file_name));

% Формируем поле допустимых значений
scope_M = def_scope_list(main_M, mod, N);

% Первоначальная инициализация поля и переменной ошибки
current_M = init_field(main_M, scope_M, N);
current_Err = error(current_M, mod, N);

%% Метод имитации отжига

% Подготовка
new_M = current_M;
new_Err = current_Err;
T = T_0;
step = 1;
note_T = zeros(1, step_N);
note_Err = zeros(1, step_N);

% Основной цикл
while (step < step_N) && (current_Err > 0)
    
    % Ведем запись
    note_T(step) = T;
    note_Err(step) = current_Err;
    
    % Формируем новую конфигурацию
    new_M = outrage(main_M, scope_M, current_M, N, num);
    new_Err = error(new_M, mod, N);
    delta = new_Err - current_Err;
    
    % Осуществляем переход к новой конфигурации
    if is_rand_choice(transit_probability(T, delta, k_p))
        current_Err = new_Err;
        current_M = new_M;
    end
    
    % Понижаем температуру обновляем шаг
    T = temperature(T_0, step, k_T);
    step = step + 1;
end

%% Вывод данных

fprintf('\n## Результат :\n\n');
disp(current_M);
disp([' > Число коллизий : ', num2str(current_Err)]);
disp([' > Температура    : ', num2str(T)]);
disp([' > Шаг            : ', num2str(step)]);

%% Функции

% Находит допустимые значения для данной клетки
function var = def_scope(M, v, mod, N)

    % Инициализация
    temp = zeros(1, N, 'int16');
    var = [];
    x = int16(0);
    y = int16(0);

    % Поиск элементов по строке и столбцу
    for i = int16(1:N)
        if M(i, v(2)) > 0
            temp(M(i, v(2))) = 1;
        end
        if M(v(1), i) > 0
            temp(M(v(1), i)) = 1;
        end
    end

    % Поиск координат соответствующего квадрата (mod х mod)
    if rem(v(1), mod) > 0
        x = mod * idivide(v(1), mod);
    else
        x = mod * (idivide(v(1), mod) - 1);
    end
    if rem(v(2), mod) > 0
        y = mod * idivide(v(2), mod);
    else
        y = mod * (idivide(v(2), mod) - 1);
    end

    % Поиск элементов в соответствующем квадрате (mod х mod)
    for i = int16(1:mod)
        for j = int16(1:mod)
            if M(i + x, j + y) > 0
                temp(M(i + x, j + y)) = 1;
            end
        end
    end

    % Запись элементов, которые не были встречены
    for i = int16(1:N)
        if temp(i) == 0
            var = [var, i];
        end
    end
end

% Создает список допустимых значений
function list = def_scope_list(M, mod, N)
    list = cell(N, N);
    for i = int16(1:N)
        for j = int16(1:N)
            if M(i, j) == 0
                list{i, j} = def_scope(M, [i, j], mod, N);
            else
                list{i, j} = NaN;
            end
        end
    end
end

% Возвращает случайный элемент из массива
function v = get_rand(array)
    v = array(randi(size(array, 2)));
end

% Производит инициализацию конфигурации
function C = init_field(M, S, N)
    C = M;
    for i = int16(1:N)
        for j = int16(1:N)
            if C(i, j) == 0
                C(i, j) = get_rand(S{i, j});
            end
        end
    end
end

% Производит случайную модификацию текущей конфигурации
function D = outrage(M, S, C, N, num)
    D = C;
    for i = 1:num
        v = randi(N, [1, 2]);
        while M(v(1), v(2)) > 0
            v = randi(N, [1, 2]);
        end
        D(v(1), v(2)) = get_rand(S{v(1), v(2)});
    end
end

% Функция ошибки (оптимизационная функция)
function v = error(D, mod, N)
    
    % Проход по строкам и столбцам
    v = 0;
    for i = int16(1:N)
        temp1 = zeros(1, N);
        temp2 = zeros(1, N);
        for j = 1:N
            temp1(D(i, j)) = 1;
            temp2(D(j, i)) = 1;
        end
        v = v + sum(temp1 == 0) + sum(temp2 == 0);
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
function T = temperature(T_0, step, k)
    T = T_0 * exp(-k * step);
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