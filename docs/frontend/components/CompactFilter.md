# CompactFilter

Активный фильтр dashboard для выбора символа и бирж.

## Реальная реализация

```text
frontend/FundingMonitor.Web/src/features/filters/CompactFilter.tsx
```

Compatibility export:

```text
frontend/FundingMonitor.Web/src/components/CompactFilter.tsx
frontend/FundingMonitor.Web/src/components/index.ts
```

## Props

```ts
export interface CompactFilterProps {
  selectedExchanges: ExchangeType[];
  onExchangesChange: (exchanges: ExchangeType[]) => void;
  selectedSymbol: string;
  onSymbolChange: (symbol: string) => void;
  availableSymbols: string[];
}
```

## Поведение

- Поиск символа по `availableSymbols`.
- Коммит символа по `Enter`.
- Закрытие dropdown по `Escape` или клику снаружи.
- Выбор одной или нескольких бирж.
- Быстрые действия: выбрать все биржи, сбросить выбор.
- Если биржи не выбраны, это трактуется как "все биржи".

## Заменил legacy

`CompactFilter` заменяет удалённые `CoinSelector` и `ExchangeSelector`.

## Связанные заметки

- [[index|Frontend Components]]
- [[../../architecture/index|Архитектура]]
