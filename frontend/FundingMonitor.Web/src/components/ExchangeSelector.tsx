import React from 'react';
import type { ExchangeType } from '../types';

interface ExchangeSelectorProps {
  selectedExchanges: ExchangeType[];
  onExchangesChange: (exchanges: ExchangeType[]) => void;
}

const exchanges: ExchangeType[] = ['Binance', 'Bybit', 'OKX'];

export const ExchangeSelector: React.FC<ExchangeSelectorProps> = ({
  selectedExchanges,
  onExchangesChange,
}) => {
  const toggleExchange = (exchange: ExchangeType) => {
    if (selectedExchanges.includes(exchange)) {
      onExchangesChange(selectedExchanges.filter((e) => e !== exchange));
    } else {
      onExchangesChange([...selectedExchanges, exchange]);
    }
  };

  const selectAll = () => onExchangesChange([...exchanges]);
  const deselectAll = () => onExchangesChange([]);

  return (
    <div className="flex flex-col gap-2">
      <label className="text-sm font-medium" style={{ color: 'var(--tg-text-secondary)' }}>Биржи</label>
      <div className="flex flex-wrap gap-2">
        {exchanges.map((exchange) => (
          <button
            key={exchange}
            onClick={() => toggleExchange(exchange)}
            className="px-4 py-2 rounded-xl font-medium transition-all duration-200"
            style={{
              backgroundColor: selectedExchanges.includes(exchange) ? 'var(--tg-button)' : 'var(--tg-bg-tertiary)',
              color: selectedExchanges.includes(exchange) ? 'var(--tg-button-text)' : 'var(--tg-text-secondary)',
              border: selectedExchanges.includes(exchange) ? '1px solid var(--tg-button)' : '1px solid var(--tg-border)',
            }}
          >
            {exchange}
          </button>
        ))}
      </div>
      <div className="flex gap-2 mt-1">
        <button
          onClick={selectAll}
          className="text-xs transition-colors"
          style={{ color: 'var(--tg-link)' }}
        >
          Выбрать все
        </button>
        <button
          onClick={deselectAll}
          className="text-xs transition-colors"
          style={{ color: 'var(--tg-text-tertiary)' }}
        >
          Сбросить
        </button>
      </div>
    </div>
  );
};
