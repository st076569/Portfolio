%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% Вычислительный практикум №1. Баталов Семен (351), 2022.
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

% Функция нахождения корня на отрезке модифицированным методом Ньютона
function [x_0, x, nIter, dev, isErr] = newton_m(f_0, f_1, abs_f_1, ...
    epsilon, delta, ab, kMax, nTryMax)
    
    % Находим минимум модуля производной
    [~, m1] = fminbnd(abs_f_1, ab(1), ab(2));
    
    % Проверяем, что минимум не подходит близко к нулю
    if (m1 < delta)
        
        % Основное условие было нарушено
        isErr = true;
        x_0 = 0;
        x = 0;
        nIter = 0;
        dev = 0;
    else
        
        % Основное условие было выполнено
        isErr = false;
        haveFound = false;
        counter = 0;
        
        % Подбираем стартовую точку так, чтобы метод сошелся
        while (~haveFound) && (counter < nTryMax)
            
            % Выбираем случайную стартовую точку
            counter = counter + 1;
            x_0 = ab(1) + (ab(2) - ab(1)) * rand;
            x_old = x_0;
            x_new = x_0;
            nIter = 0;
            
            % Проводим итерации, пока не достигнем точности epsilon
            while (abs(f_0(x_new)) / m1 > epsilon) && (kMax > nIter)
                nIter = nIter + 1;
                x_old = x_new;
                x_new = x_old - f_0(x_old) / f_1(x_0);
            end
            
            % Проверяем, верно ли была выбрана стартовая точка
            if (kMax > nIter)
                haveFound = true;
                x = x_new;
                dev = abs(f_0(x));
            end
        end
        
        % Если не была найдена подходящая точка 
        if (~haveFound)
            isErr = true;
            x_0 = 0;
            x = 0;
            nIter = 0;
            dev = 0;
        end
    end
end