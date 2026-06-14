# ADR 0005: RFC 7807 ProblemDetails для глобальной обработки ошибок

**Статус**: Принято  
**Дата**: 2026-06-14

## Контекст

Нужно единообразное форматирование ошибок API:
- Валидация входных параметров (400)
- Не найдено (404)
- Внешние API недоступны (503)
- Внутренние ошибки (500)
- Таймауты (504)

ASP.NET Core имеет встроенную поддержку `ProblemDetails` (RFC 7807).

## Решение

Кастомный `ExceptionHandlingMiddleware` + иерархия `ProblemDetails`:

```
ProblemDetails (abstract, RFC 7807)
├── ValidationProblemDetails (400) — ошибки валидации полей
├── NotFoundProblemDetails (404) — ресурс не найден
├── ServiceUnavailableProblemDetails (503) — внешние зависимости недоступны
├── TimeoutProblemDetails (504) — таймаут внешнего API
└── ServerProblemDetails (500) — непредвиденные ошибки
```

Middleware ловит все исключения, мапит на соответствующий `ProblemDetails`, логирует, возвращает JSON.

Пример ответа 400:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "symbol": ["Symbol is required"]
  }
}
```

## Последствия

✅ **Плюсы**:
- Стандарт RFC 7807 — понятен клиентам, есть библиотеки для парсинга
- Единый формат для всех ошибок
- Swagger/OpenAPI автоматически документирует схемы
- Расширяемо: можно добавить `detail`, `instance`, custom поля

❌ **Минусы**:
- Дополнительный boilerplate (middleware, классы)
- Нужно дисциплинировано кидать правильные исключения

## Альтернативы

| Вариант | Причина отказа |
|---------|----------------|
| Просто `return BadRequest("message")` | Нет структуры, сложно парсить на клиенте, не документируется в Swagger |
| Кастомный `ApiResponse<T>` wrapper | Не стандарт, дублирует HTTP статус коды |
| `Hellang.Middleware.ProblemDetails` (пакет) | Не нужен — встроенная поддержка ASP.NET Core 8+ достаточна |

## Примечание

Для валидации DTO используем `FluentValidation` (если подключим) или ручную проверку в контроллерах с `throw new ArgumentException(...)` — middleware обернёт в `ValidationProblemDetails`.