% Формирует трехдиагональную матрицу и столбец свободных членов
function [K, F] = create_system(p, r, f, N, h)
    B = (integrate_der_ii(p, 0:h:(1 - 2 * h), h) + ...
        integrate_phi_ii(r, 0:h:(1 - 2 * h), h))';
    A = (integrate_der_ij(p, 0:h:(1 - 3 * h), h) + ...
        integrate_phi_ij(r, 0:h:(1 - 3 * h), h))';
    K = spdiags([[A; 0], B, [0; A]], [-1, 0, 1], N - 1, N - 1);
    F = (integrate_phi_i(f, 0:h:(1 - 2 * h), h))';
end

% Схема совпадения базисных функций i = j
function int = integrate_phi_ii(f, x0, h)
    int = 2 / 3 * f(x0 + h) * h;
end

% Схема для одной базисной функций i
function int = integrate_phi_i(f, x0, h)
    int = f(x0 + h) * h;
end

% Схема со сдвигом базисных функций j = i + 1
function int = integrate_phi_ij(f, x0, h)
    int = 1 / 6 * f(x0 + 1.5 * h) * h;
end

% Схема совпадения производных базисных функций i = j
function int = integrate_der_ii(f, x0, h)
    int = (f(x0 + 0.5 * h) + f(x0 + 1.5 * h)) / h;
end

% Схема со сдвигом производных базисных функций j = i + 1
function int = integrate_der_ij(f, x0, h)
    int = -f(x0 + 1.5 * h) / h;
end