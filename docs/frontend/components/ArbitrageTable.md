# ArbitrageTable

Таблица арбитражных возможностей по funding rate с группировкой по символам, раскрывающимися строками и сортировкой.

## Путь
`frontend/FundingMonitor.Web/src/components/ArbitrageTable.tsx`

## Props

```typescript
interface ArbitrageTableProps {
  data: FundingArbitrageDto[];                              // Все арбитражные пары
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void; // Клик по строке
}
```

## Структура данных (FundingArbitrageDto)

```typescript
{
  symbol: string;           // "BTC-USDT"
  exchangeA: string;        // "Binance"
  exchangeB: string;        // "Bybit"
  priceA: number;           // Mark Price на A
  priceB: number;           // Mark Price на B
  priceSpread: number;      // PriceA - PriceB ($)
  priceSpreadPercent: number; // Спред в %
  fundingRateA: number;     // Funding rate на A
  fundingRateB: number;     // Funding rate на B
  fundingRateSpread: number; // Разница rateA - rateB
  paymentsA: number;        // Выплат/день на A
  paymentsB: number;        // Выплат/день на B
  shortExchange: string;    // Биржа для Short (выше APR)
  longExchange: string;     // Биржа для Long (ниже APR)
  aprSpread?: number;       // Разница APR (может отсутствовать)
}
```

## Функциональность

### Группировка по символам
- Все записи группируются по `symbol` (`Map<string, FundingArbitrageDto[]>`)
- Внутри группы **уже отсортированы** по выбранному критерию
- Первая запись = **Best** (лучшая связка), остальные = **Others**
- Рендерится как: Best (всегда видно) + Others (при раскрытии)

### Сортировка (global, до группировки)
Клик по заголовку колонки:
| Колонка | Поле для сортировки |
|---------|---------------------|
| **Спред цены** | `priceSpreadPercent` |
| **Спред фандинга** | `fundingRateSpread * 100` |
| **Доходность (APR)** | `Math.abs(aprSpread)` |
| **Символ** | `symbol` (localeCompare) |

Цикл: `asc` → `desc` → `none` (оригинальный порядок)

### Раскрытие деталей (Expand)
- Кнопка ▼/▲ рядом с символом в строке Best
- При клике показываются **Others** для этого символа (все остальные пары бирж)
- `expandedSymbols` — `Set<string>` в состоянии

### Отображение строки (2 строки на пару)
| Ряд | Содержимое |
|-----|------------|
| **Row 1** | Символ (rowSpan=2) | Биржа A + L/S badge | Цена A | Спред цены (rowSpan=2) | Funding Rate A | Спред фандинга (rowSpan=2) |
| **Row 2** | — | Биржа B + L/S badge | Цена B | — | Funding Rate B | — |

**Цветовая индикация**:
- Биржа: цветной бейдж (Binance/Bybit/OKX)
- L/S: **Зелёный** = Long (получаем funding), **Красный** = Short (платим funding)
- Спред фандинга: зелёный (профит), красный (убыток)
- Funding Rate: зелёный/красный/серый по знаку

### Форматирование чисел
| Поле | Формат |
|------|--------|
| Цены | `$` + locale, 4-8 знаков |
| Спред цены | `%` (4 знака) + `$` абсолютный |
| Funding Rate | `%` (0-4 знака) + `{N} вып./день` |
| Спред фандинга | `%` (0-4 знака) |

### UI детали
- Sticky заголовок + sticky колонка "Символ"
- `min-width: 860px` — горизонтальный скролл на мобильных
- Hover эффект на строках (`cursor-pointer`)
- Пустое состояние: "Нет арбитражных возможностей"
- Легенда под таблицей с расшифровкой L/S, спреда, APR

## Хелперы

```typescript
const calcFundingSpread = (item) => (item.fundingRateSpread ?? item.fundingRateA - item.fundingRateB) * 100;
const calcFundingRate = (rate) => rate * 100;
const getProfitability = (item) => Math.abs(item.aprSpread);
```

## Зависимости
- `FundingArbitrageDto` из `../types`
- `getExchangeColorClass` — локальная функция для CSS классов бирж

## Пример использования

```tsx
<ArbitrageTable
  data={arbitrageData}
  onArbitrageClick={(symbol, exchanges) => {
    // Переключает главные фильтры на этот символ и биржи
    setMainFilters({ symbol, exchanges });
  }}
/>
```

## Особенности
- **Smart grouping**: сортировка → группировка → рендер Best + expandable Others
- RowSpan для ячеек, общих для двух строк пары
- `onArbitrageClick` вызывается и на Best, и на Others строках
- Иконки сортировки в заголовках (SVG inline)