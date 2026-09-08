# HistoryTable

APR table по историческим funding rates.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/historical-rates/HistoryAprTable.tsx
```

## Props

```ts
interface HistoryTableProps {
  selectedExchanges: ExchangeType[];
  symbol: string;
}
```

## Поведение

- Загружает APR statistics через `useAprStats`.
- Показывает периоды из `PERIODS`: 1, 2, 3, 7, 14, 21, 30 дней.
- Фильтрует данные по выбранным биржам.
- Сортирует строки бирж по APR выбранного периода.
- Показывает APR, суммарную funding rate, число выплат, среднюю ставку и standard deviation.

## Связанные заметки

- [[HistoryPanel]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
