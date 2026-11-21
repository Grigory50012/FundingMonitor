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
  exchangeOrder: ExchangeType[];
  sortConfig: CurrentRatesSortConfig;
  onSortChange: (sort: CurrentRatesSortConfig) => void;
}
```

## Поведение

- Общий список бирж и сортировку задаёт `useDashboardData` через `fundingExchangeOrder.ts`. Порядок совпадает с APR-таблицей: по умолчанию Binance, Bybit, OKX; сортировка любой из таблиц переставляет обе.
- Поддерживает сортировку по `markPrice`, `fundingRate`, `nextFundingTime`, `apr` через `onSortChange`.
- Если данных биржи нет, сохраняет её строку с прочерками, чтобы не сдвигать строки относительно истории.
- Общий класс `funding-rates-table` задаёт высоту заголовка 36 px и строк 64 px (переменная `--funding-row-height` в `index.css`).
- Показывает exchange badge и ссылку на торговую страницу биржи.
- Использует `EmptyState`, если данных нет.
- Общий таймер `useDashboardData` каждые 5 секунд одновременно запускает фоновые запросы текущих ставок и арбитража. Таблица остаётся смонтированной, сохраняет сортировку и прокрутку; индикатор загрузки при автообновлении не показывается.
- В скрытой вкладке общий опрос приостанавливается. При возвращении во вкладку или восстановлении сети обе таблицы запрашиваются одним событием; на паузе арбитража обновляются только текущие ставки.
- Пока запрос выполняется, очередное автообновление пропускается. При смене фильтров или размонтировании устаревший запрос отменяется.
- При ошибке обновления сохраняются последние полученные данные; следующая попытка выполняется по таймеру.

## Модель

Логика фильтрации и сортировки вынесена в:

```text
src/features/current-rates/currentRatesTableModel.ts
```

## Связанные заметки

- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
