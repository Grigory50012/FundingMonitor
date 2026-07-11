# CurrentDataTable

Compatibility name для текущей таблицы funding rates.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/current-rates/CurrentRatesTable.tsx
```

Compatibility export:

```text
frontend/FundingMonitor.Web/src/components/CurrentDataTable.tsx
frontend/FundingMonitor.Web/src/components/index.ts
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
