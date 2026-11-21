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
  data: AprPeriodStatsDto[];
  isLoading: boolean;
  error: string | null;
  hasData: boolean;
  exchangeOrder: ExchangeType[];
  sortConfig: HistoryAprSortConfig;
  onSortChange: (sort: HistoryAprSortConfig) => void;
}
```

## Поведение

- Получает APR statistics от `useDashboardData`, который вызывает `useAprStats` при активном режиме таблицы.
- Использует TanStack Query с тем же расписанием `HH:01`, что и график истории. При возвращении во вкладку или восстановлении сети обновляет устаревшие данные.
- Фоновое обновление сохраняет таблицу, сортировку и прокрутку; при ошибке показывает сообщение рядом с последними полученными данными, выполняет до трёх повторных попыток.
- Показывает периоды из `PERIODS`: 1, 2, 3, 7, 14, 21, 30 дней.
- Фильтрует данные по выбранным биржам.
- Порядок бирж общий с таблицей текущих ставок. Сортировка по APR выбранного периода или колонке текущих ставок синхронно переставляет строки обеих таблиц; отсутствующие значения отображаются прочерками.
- Общая высота заголовка — 36 px, строк — 64 px; место под заголовки частей панели одинаковое. На узком экране APR-таблица допускает горизонтальную прокрутку, не увеличивая высоту строк из-за переносов.
- Показывает APR, суммарную funding rate, число выплат и среднюю ставку.
- Пояснения к метрикам доступны через значок «?» рядом с заголовком «История / APR по периодам» при наведении или фокусе с клавиатуры.

## Связанные заметки

- [[HistoryPanel]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
