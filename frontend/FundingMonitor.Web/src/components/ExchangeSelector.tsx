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
      <label className="text-sm font-medium text-gray-300">Биржи</label>
      <div className="flex flex-wrap gap-2">
        {exchanges.map((exchange) => (
          <button
            key={exchange}
            onClick={() => toggleExchange(exchange)}
            className={`px-4 py-2 rounded-lg font-medium transition-all duration-200 
                       ${
                         selectedExchanges.includes(exchange)
                           ? 'bg-blue-600 text-white shadow-lg shadow-blue-600/30'
                           : 'bg-gray-800 text-gray-400 border border-gray-700 hover:border-gray-600'
                       }`}
          >
            {exchange}
          </button>
        ))}
      </div>
      <div className="flex gap-2 mt-1">
        <button
          onClick={selectAll}
          className="text-xs text-blue-400 hover:text-blue-300 transition-colors"
        >
          Выбрать все
        </button>
        <button
          onClick={deselectAll}
          className="text-xs text-gray-500 hover:text-gray-400 transition-colors"
        >
          Сбросить
        </button>
      </div>
    </div>
  );
};
