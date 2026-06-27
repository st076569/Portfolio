% Реализует метод итераций Якоби
function [U, T] = jacoby_algorithm(K, F, N, sigma, err, critNum)

    % Подготовка
    D = spdiags(diag(K), 0, N - 1, N - 1) ^ -1;
    U = zeros(N - 1, 1);

    % Проходим определенное количество итераций
    tic
    for i = 1:1:critNum

        % Сдвиг к новому решению
        delta = sigma * D * (K * U - F);

        % Проверка расстояния Чебышева
        if (max(abs(delta)) > err)
            U = U - delta;
        else
            break;
        end
    end
    T = toc;
end