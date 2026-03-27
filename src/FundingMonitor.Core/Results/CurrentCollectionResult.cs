namespace FundingMonitor.Core.Results;

/// <summary>
///     Результат сбора текущих данных
/// </summary>
/// <param name="Success">Успешно ли выполнена операция</param>
/// <param name="ErrorMessage">Сообщение об ошибке</param>
/// <param name="RatesCount">Количество собранных ставок</param>
/// <param name="EventsCount">Количество обнаруженных событий (изменений)</param>
public record CurrentCollectionResult(
    bool Success,
    string? ErrorMessage = null,
    int RatesCount = 0,
    int EventsCount = 0
);