# CurrentDataTable

Таблица текущих funding rates.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/current-rates/CurrentRatesTable.tsx
```

## Props

```ts
interface CurrentRatesTableProps {
  data: FundingRateDto[];
  selectedExchanges: ExchangeType[];
}
```

## Поведение

- Фильтрует данные по выбранным биржам.
- Сортирует по `markPrice`, `fundingRate`, `nextFundingTime`, `apr`.
- Показывает exchange badge и ссылку на торговую страницу биржи.
- Использует `EmptyState`, если данных нет.

## Модель

Логика фильтрации и сортировки вынесена в:

```text
src/features/current-rates/currentRatesTableModel.ts
```

## Связанные заметки

- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
