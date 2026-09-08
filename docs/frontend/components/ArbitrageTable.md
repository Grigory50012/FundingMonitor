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
- При клике может синхронизировать основной фильтр dashboard через `onArbitrageClick`.
- Показывает exchange links `exchangeAUrl` и `exchangeBUrl`.

## Связанные заметки

- [[CompactFilter]]
- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
