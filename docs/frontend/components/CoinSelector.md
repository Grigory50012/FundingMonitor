# CoinSelector

> **Legacy**: компонент экспортируется, но в текущем UI не используется. Фильтрация монет реализована в `CompactFilter`.

Выпадающий селектор монеты с поиском.

## Путь
`frontend/FundingMonitor.Web/src/components/CoinSelector.tsx`

## Props

```typescript
interface CoinSelectorProps {
  selectedCoin: string;           // Текущий выбранный символ (например, "BTC")
  onCoinChange: (coin: string) => void;
  availableCoins: string[];       // Список всех монет для выбора
}
```

## Функциональность

### Поиск
- Инпут с автофокусом при открытии
- Фильтрация `availableCoins` по `includes` (case-insensitive)
- Очистка поиска: крестик в инпуте
- **Лимит отображения**: 100 монет (`displayCoins = filteredCoins.slice(0, 100)`)
- Инфо-строка если отфильтровано > 100: "Показано 100 из N монет (уточните поиск)"

### Выбор
- Клик по монете → `onCoinChange(coin)`, закрытие дропдауна, очистка поиска
- Выбранная монета: голубой фон `rgba(0, 136, 204, 0.15)` + галочка + цвет `var(--tg-link)`

### Дропдаун
- Портал через `fixed inset-0` оверлей (закрытие по клику вне)
- Контейнер: `absolute z-20 w-80 max-h-96 overflow-auto`
- Поиск всегда виден (sticky вверху контейнера)
- Список: кнопки полной ширины с ховер/актив состоянием

### Кнопка-триггер
- Полная ширина (`w-full min-w-[200px]`)
- Плейсхолдер: "Выберите монету" если пусто
- Стрелка вращается на 180° при открытии
- Стили: `var(--tg-bg-tertiary)` фон, `var(--tg-border)` граница

## UI/UX
- Тёмная тема через CSS переменные
- Плавные переходы (`transition-all`)
- Тени: `shadow-2xl` на контейнере дропдауна
- Скролл только внутри списка (`max-h-96 overflow-auto`)

## Зависимости
- Только React hooks (`useState`, `useMemo`)
- SVG иконки inline (поиск, крестик, стрелка, галочка)

## Пример использования

```tsx
<CoinSelector
  selectedCoin={arbitrageFilters.symbol}
  onCoinChange={(coin) => setArbitrageFilters({ ...arbitrageFilters, symbol: coin })}
  availableCoins={availableCoins}
/>
```

## Ограничения
- **Hardcoded лимит 100** — для производительности рендера
- Нет виртуализации списка (react-window)
- Нет клавиатурной навигации по списку (Enter/ArrowUp/ArrowDown)
- Нет группировки/категоризации монет

## Возможные улучшения
1. Добавить `react-window` для виртуализации
2. Клавиатурная навигация (доступность)
3. Группировка: "Популярные", "DeFi", "Meme" и т.д.
4. Недавние/избранные монеты вверху списка
5. Дебаунс поиска (сейчас мгновенный useMemo)