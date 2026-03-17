import React from 'react';

interface CoinSelectorProps {
  selectedCoin: string;
  onCoinChange: (coin: string) => void;
  availableCoins: string[];
}

export const CoinSelector: React.FC<CoinSelectorProps> = ({
  selectedCoin,
  onCoinChange,
  availableCoins,
}) => {
  return (
    <div className="flex flex-col gap-2">
      <label className="text-sm font-medium text-gray-300">Монета</label>
      <select
        value={selectedCoin}
        onChange={(e) => onCoinChange(e.target.value)}
        className="bg-gray-800 border border-gray-700 rounded-lg px-4 py-2 text-white 
                   focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                   hover:border-gray-600 transition-colors cursor-pointer"
      >
        <option value="" disabled>Выберите монету</option>
        {availableCoins.map((coin) => (
          <option key={coin} value={coin}>
            {coin}
          </option>
        ))}
      </select>
    </div>
  );
};
