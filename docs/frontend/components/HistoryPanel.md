# HistoryPanel

Compatibility name для графика исторических funding rates.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/history/HistoryChartPanel.tsx
```

Compatibility export:

```text
frontend/FundingMonitor.Web/src/components/HistoryPanel.tsx
frontend/FundingMonitor.Web/src/components/index.ts
```

## Props

```ts
interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
  timeRange: TimeRangeType;
  onTimeRangeChange: (range: TimeRangeType) => void;
}
```

## Поведение

- Показывает историю funding rates через Recharts.
- Фильтрует данные по выбранным биржам и time range.
- Поддерживает переключение временного диапазона.
- Модель подготовки данных вынесена в `historyChartModel.ts`.

## Связанные заметки

- [[HistoryTable]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
