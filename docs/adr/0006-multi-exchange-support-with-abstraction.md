# ADR 0006: Абстракция клиентов бирж для поддержки множественных источников

**Статус**: Принято  
**Дата**: 2026-06-14

## Контекст

Проект работает с 3 биржами: Binance, Bybit, OKX. У каждой своё API, форматы ответов, rate limits, ошибки. Нужно:
- Единый интерфейс для получения текущих и исторических ставок
- Легкое добавление новых бирж
- Изоляция ошибок одной биржи от других
- Параллельный опрос

## Решение

**Interface-based abstraction** в `Infrastructure/ExchangeClients`:

```csharp
// Core.Interfaces.Clients
public interface IExchangeFundingRateClient
{
    Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken ct);
    Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(string symbol, DateTime from, DateTime to, CancellationToken ct);
    ExchangeType Exchange { get; }
}

// Infrastructure — реализации
public class BinanceFundingRateClient : IExchangeFundingRateClient { ... }
public class BybitFundingRateClient : IExchangeFundingRateClient { ... }
public class OkxFundingRateClient : IExchangeFundingRateClient { ... }
```

В `CurrentFundingRateCollector` — `IEnumerable<IExchangeFundingRateClient>` инжектируется через DI, опрос параллельный через `Task.WhenAll`.

Ошибки одной биржи логируются и не падают весь цикл (`try-catch` на каждом клиенте).

## Последствия

✅ **Плюсы**:
- Open/Closed Principle: новая биржа = новый класс + регистрация в DI
- Тестируемость: легко замокать `IFundingRateClient`
- Изоляция: сбой Binance не блокирует Bybit/OKX
- Единая модель домена (`CurrentFundingRate`, `HistoricalFundingRate`)

❌ **Минусы**:
- Дублирование кода маппинга ответов в доменные модели
- Нужно поддерживать совместимость при изменении API бирж

## Альтернативы

| Вариант | Причина отказа |
|---------|----------------|
| Один класс с `switch (exchange)` | Нарушает SRP, сложно тестировать, растущий God Object |
| Генерация клиентов из OpenAPI схем | Биржи часто не предоставляют полные спеки для funding rates |
| gRPC / Protobuf между сервисами | Overkill, биржи не предоставляют gRPC |

## Примечание

Для rate limiting и retry политик — можно добавить `Polly` политики в DI регистрацию каждого клиента (`AddHttpClient<BinanceFundingRateClient>().AddTransientHttpErrorPolicy(...)`).