%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% Вычислительный практикум №1. Баталов Семен (351), 2022.
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

% Функция вывода результатов метода секущих
function print_sechant_method(f_0, abs_f_1, epsilon, delta, g, kMax, name)
    
    % Открываем файл для записи
    fileId = fopen(name, 'a');
    fprintf(fileId, '\n## Метод секущих');
    
    % Расчет корней для всех промежутков
    for i = 1:size(g, 1)
        
        % Расчет требуемых значений
        [x_1, x_2, x, nIter, dev, isErr] = sechant_method(f_0, ...
            abs_f_1, epsilon, delta, g(i, 1:2), kMax);
        
        % Вывод расчета в файл
        fprintf(fileId, '\nПромежуток : [%.4f, %.4f]', g(i, 1), g(i, 2));
        fprintf(fileId, '\n > Стартовые точки  : {%.4e, %.4e}', ...
            x_1, x_2);
        fprintf(fileId, '\n > Корень           : %.12e', x);
        fprintf(fileId, '\n > Число итераций   : %d', nIter);
        fprintf(fileId, '\n > Невязка          : %.12e', dev);
        
        % Анализ ошибки
        if (isErr)
            fprintf(fileId, '\n > Ошибка           : %s', isErr);
        end
    end
    fprintf(fileId, '\n');
    
    % Закрываем файл для записи
    fclose(fileId);
end