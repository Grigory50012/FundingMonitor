# ExchangeSelector

> **Legacy**: компонент экспортируется, но в текущем UI не используется. Выбор бирж реализован в `CompactFilter`.

Простой компонент выбора бирж в виде чекбокс-кнопок (toggle buttons).

## Путь
`frontend/FundingMonitor.Web/src/components/ExchangeSelector.tsx`

## Props

```typescript
interface ExchangeSelectorProps {
  selectedExchanges: ExchangeType[];        // Текущий выбор
  onExchangesChange: (exchanges: ExchangeType[]) => void;
}
```

## Функциональность

### Отображение
- 3 кнопки в `flex-wrap gap-2`: **Binance**, **Bybit**, **OKX**
- Состояние кнопки:
  - **Выбрана**: фон `var(--tg-button)`, текст `var(--tg-button-text)`, граница `var(--tg-button)`
  - **Не выбрана**: фон `var(--tg-bg-tertiary)`, текст `var(--tg-text-secondary)`, граница `var(--tg-border)`
- Переходы: `transition-all duration-200`

### Действия
- Клик по кнопке →.toggle (добавить/удалить из массива)
- **"Выбрать все"**: устанавливает все 3 биржи
- **"Сбросить"**: очищает выбор

### Стили
- `rounded-xl px-4 py-2` — компактные кнопки
- `font-medium` — средний вес шрифта
- Цвета ссылок: `var(--tg-link)` для "Выбрать все", `var(--tg-text-tertiary)` для "Сбросить"

## Константы

```typescript
const exchanges: ExchangeType[] = ['Binance', 'Bybit', 'OKX'];
```

## Зависимости
- `ExchangeType` из `../types`
- Только React (`useState` не нужен — всё через пропсы)

## Пример использования

```tsx
<ExchangeSelector
  selectedExchanges={filters.exchanges}
  onExchangesChange={setExchanges}
/>
```

## Особенности
- **Статичный список** бирж (hardcoded) — при добавлении новой биржи нужно править компонент
- Нет дропдауна — все варианты сразу видны (подходит для 3-х вариантов)
- Простая альтернатива `CompactFilter` когда не нужен поиск монеты

## Сравнение с CompactFilter

| Аспект | ExchangeSelector | CompactFilter (Exchange часть) |
|--------|------------------|-------------------------------|
| UI | Кнопки в ряд | Дропдаун с чекбоксами |
| Компактность | Шире (3 кнопки) | Уже (1 кнопка-триггер) |
| Видимость | Все сразу | При клике |
| Поиск | Нет | Нет (только 3 варианта) |
| "Все/Сброс" | Отдельные кнопки | В дропдауне |
| Использование | Арбитраж фильтры (старое?) | Главный дашборд + арбитраж |

> ⚠️ **Примечание**: В `DashboardContainer.tsx` используется `CompactFilter` для обоих фильтров. `ExchangeSelector` может быть legacy или для альтернативного UI.