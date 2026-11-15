# ArbitrageTable

Таблица funding arbitrage opportunities.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/arbitrage/ArbitrageTable.tsx
```

## Props

```ts
interface ArbitrageTableProps {
  data: FundingArbitrageDto[];
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void;
}
```

## Поведение

- Группирует opportunities по символу.
- Показывает лучшую связку первой.
- Позволяет раскрывать дополнительные связки по тому же символу.
- Сортирует по `profitabilityPercent`, `priceSpreadPercent`, `fundingRateSpread`, `symbol`.
- Монета, биржи, минимальный APR, лимит неблагоприятного спреда по ценам маркировки и переключатель одинаковых интервалов передаются вместе в `GET /api/v1/Arbitrage`. При открытии экрана и после сброса критерии пусты.
- Основная пара группы выбирается по APR среди пар из ответа. При неизвестных расчётных значениях таблица показывает прочерк.
- При клике может синхронизировать основной фильтр dashboard через `onArbitrageClick`.
- Показывает exchange links `exchangeAUrl` и `exchangeBUrl`.

## Связанные заметки

- [[CompactFilter]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
