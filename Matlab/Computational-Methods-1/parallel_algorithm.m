% Решает задачу методом прогонки для нескольких подобластей
function [U, T] = parallel_algorithm(K, F, N)
    
    % Временные переменные
    tic
    C = K;
    A = [K, F];
    n = sqrt(N);
    M = zeros(2 * n - 1, 2 * n - 1);

    % Формируем конечный вид блочно-диагональной матрицы СЛУ
    for i = 1:1:(n - 1)
        C(i * n, i * n) = 1;
        C(i * n, i * n + 1) = 0;
        C(i * n, i * n - 1) = 0;
        C(i * n + 1, i * n) = 0;
        C(i * n - 1, i * n) = 0;
    end
    for i = 1:n:(N + 1 - 2 * n)
        M = C(i:i + 2 * (n - 1), i:i + 2 * (n - 1)) * ...
                A(i:i + 2 * (n - 1), i:i + 2 * (n - 1)) ^ -1;
        if (i < (N + 1 - 2 * n))
            A(i:i + 2 * (n - 1), i + 2 * (n - 1) + 1) = M * ...
                A(i:i + 2 * (n - 1), i + 2 * (n - 1) + 1);
        end
        A(i:i + 2 * (n - 1), end) = M * A(i:i + 2 * (n - 1), end);
        A(i:i + 2 * (n - 1), i:i + 2 * (n - 1)) = ...
                C(i:i + 2 * (n - 1), i:i + 2 * (n - 1));
    end
    for i = (n - 1) * n:-n:2 * n
        A(1:i - n, end) = A(1:i - n, end) - A(i, end) * A(1:i - n, i);
    end

    % Применяем метод прогонки к каждому блоку
    for i = 1:n:(N - 1)
        [A(i:(i + n - 2), N), ~] = thomas_algorithm( ...
            A(i:(i + n - 2), i:(i + n - 2)), A(i:(i + n - 2), N), n);
    end
    U = full(A(1:end, N));
    T = toc;
end