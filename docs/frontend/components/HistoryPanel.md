# HistoryPanel

График исторических funding rates.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/historical-rates/HistoryChartPanel.tsx
```

## Props

```ts
interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
  timeRange?: TimeRangeType;
}
```

## Поведение

- Показывает историю funding rates через Recharts.
- Фильтрует данные по выбранным биржам и time range.
- Переключатель временного диапазона расположен справа от заголовка панели в `DashboardPage`.
- Данные загружает `useHistoryRates` через TanStack Query. Фоновое обновление запланировано на `HH:01` каждого часа; при возвращении во вкладку или восстановлении сети пропущенное обновление выполняется сразу.
- Успешно загруженные данные считаются свежими до ближайшего `HH:01`. Фоновый запрос сохраняет график на экране; после ошибки выполняются до трёх повторных попыток через 2, 4 и 8 секунд.
- История и APR обновляются автоматически; отдельной кнопки обновления нет. Скрытая вкладка не опрашивается по таймеру.
- Модель подготовки данных вынесена в `historyChartModel.ts`.

## Связанные заметки

- [[HistoryTable]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
