# CompactFilter

Компактный комбинированный фильтр: поиск монеты (autocomplete) + мультивыбор бирж. Используется в главном дашборде и арбитражной панели.

## Путь
`frontend/FundingMonitor.Web/src/components/CompactFilter.tsx`

## Props

```typescript
interface CompactFilterProps {
  selectedExchanges: ExchangeType[];        // Текущий выбор бирж
  onExchangesChange: (exchanges: ExchangeType[]) => void;
  selectedSymbol: string;                   // Текущий символ (например, "BTC")
  onSymbolChange: (symbol: string) => void;
  availableSymbols: string[];               // Список всех доступных монет для автокомплита
}
```

## Функциональность

### Поиск монеты (Autocomplete)
- **Инпут** с плейсхолдером "Поиск монеты…"
- **Debounced** фильтрация по вводу (`useMemo` на `symbolInput`)
- **Умный ранжинг** результатов:
  1. Точное совпадение (case-insensitive)
  2. Префиксное совпадение (начинается с запроса)
  3. Содержит запрос
  4. Потом по длине (короче сначала), потом алфавит
- **Лимит**: 50 вариантов в дропдауне
- **Клавиатура**:
  - `Enter` — выбрать текущий ввод
  - `Escape` — закрыть дропдаун
- **Кнопка очистки** (✕) появляется при непустом вводе
- **Синхронизация**: внешний `selectedSymbol` обновляет инпут (когда дропдаун закрыт)

### Выбор бирж (Multi-select)
- Кнопка-триггер с иконкой здания + лейбл:
  - `0` или `3` выбрано → "Все биржи"
  - `1-2` выбрано → перечислены через запятую
  - `>2` (не может быть >3, но на будущее) → "Первые 2 +N"
- **Дропдаун** (портaл через `fixed inset-0` оверлей):
  - Кнопки "Все" / "Сброс"
  - 3 чекбокс-кнопки: Binance, Bybit, OKX
  - Активные: голубой фон + галочка + цвет текста `var(--tg-link)`
- **Клик вне** — закрывает оба дропдауна (общий `mousedown` listener)

### UI/UX
- **Компактный**: высота 36px (`h-9`), горизонтальный layout
- **Tailwind + CSS переменные** для темизации
- **Z-index слои**: `z-50` дропдауны, `z-40` оверлей для бирж
- **Фокус**: рамка `var(--tg-button)` при открытом дропдауне
- **Анимации**:.rotate-180 на стрелке, transition-colors

## Константы

```typescript
const EXCHANGES: ExchangeType[] = ["Binance", "Bybit", "OKX"];
```

## Зависимости
- `ExchangeType` из `../types`
- SVG иконки inline

## Пример использования

```tsx
<CompactFilter
  selectedExchanges={mainFilters.exchanges}
  onExchangesChange={(exchanges) => setMainFilters({ ...mainFilters, exchanges })}
  selectedSymbol={mainFilters.symbol}
  onSymbolChange={(symbol) => setMainFilters({ ...mainFilters, symbol: symbol.trim() || "BTC" })}
  availableSymbols={availableCoins}
/>
```

## Особенности реализации

### Click Outside
```typescript
useEffect(() => {
  const handleClickOutside = (event: MouseEvent) => {
    if (symbolRef.current && !symbolRef.current.contains(event.target as Node)) {
      setIsSymbolOpen(false);
    }
    if (exchangeRef.current && !exchangeRef.current.contains(event.target as Node)) {
      setIsExchangeOpen(false);
    }
  };
  document.addEventListener("mousedown", handleClickOutside);
  return () => document.removeEventListener("mousedown", handleClickOutside);
}, []);
```
- `mousedown` (не `click`) — срабатывает раньше, предотвращает фокус-потерю
- Два `ref`а для двух дропдаунов

### Синхронизация внешнего символа
```typescript
useEffect(() => {
  setSymbolInput(selectedSymbol ?? "");
}, [isSymbolOpen, selectedSymbol]);
```
- Обновляет инпут только когда дропдаун **закрыт** (`isSymbolOpen` в deps)
- Предотвращает "дергание" инпута أثناء ввода

### Ранжинг предложений
```typescript
const rank = (s: string) => {
  const t = s.toLowerCase();
  if (q && t === q) return 0;           // exact
  if (q && t.startsWith(q)) return 1;   // prefix
  return 2;                              // contains
};
```

## Доступность
- Семантические `button` для триггеров
- `aria-expanded` не используется (можно добавить)
- Клавиатурная навигация: Tab, Enter, Escape
- Фокус-стили через Tailwind `focus:outline-none` + custom border