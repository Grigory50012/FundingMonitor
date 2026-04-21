import React, { useState, useMemo } from "react";

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
  const [searchQuery, setSearchQuery] = useState("");
  const [isOpen, setIsOpen] = useState(false);

  // Фильтруем монеты по поисковому запросу
  const filteredCoins = useMemo(() => {
    if (!searchQuery) return availableCoins;
    return availableCoins.filter((coin) =>
      coin.toLowerCase().includes(searchQuery.toLowerCase()),
    );
  }, [availableCoins, searchQuery]);

  // Выбираем первые 100 монет для производительности
  const displayCoins = filteredCoins.slice(0, 100);

  const handleSelectCoin = (coin: string) => {
    onCoinChange(coin);
    setSearchQuery("");
    setIsOpen(false);
  };

  return (
    <div className="relative">
      <label className="text-sm font-medium block mb-2" style={{ color: 'var(--tg-text-secondary)' }}>
        Монета
      </label>

      {/* Кнопка выбора */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full min-w-[200px] rounded-xl px-4 py-2.5 
                   focus:outline-none transition-all flex items-center justify-between gap-2"
        style={{
          backgroundColor: 'var(--tg-bg-tertiary)',
          border: '1px solid var(--tg-border)',
          color: 'var(--tg-text)',
        }}
      >
        <span className="font-semibold">
          {selectedCoin || "Выберите монету"}
        </span>
        <svg
          className={`w-4 h-4 transition-transform ${isOpen ? "rotate-180" : ""}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M19 9l-7 7-7-7"
          />
        </svg>
      </button>

      {/* Выпадающий список */}
      {isOpen && (
        <>
          {/* Затемнение фона */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />

          {/* Контейнер списка */}
          <div className="absolute z-20 mt-2 w-80 rounded-2xl shadow-2xl overflow-hidden" style={{ backgroundColor: 'var(--tg-bg-secondary)', border: '1px solid var(--tg-border)' }}>
            {/* Поиск */}
            <div className="p-3" style={{ borderBottom: '1px solid var(--tg-border)' }}>
              <div className="relative">
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Поиск монеты..."
                  className="w-full rounded-xl px-4 py-2.5 pl-10 
                             focus:outline-none placeholder-opacity-50"
                  style={{
                    backgroundColor: 'var(--tg-bg-tertiary)',
                    border: '1px solid var(--tg-border)',
                    color: 'var(--tg-text)',
                  }}
                  autoFocus
                />
                <svg
                  className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  style={{ color: 'var(--tg-hint)' }}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                  />
                </svg>
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery("")}
                    className="absolute right-3 top-1/2 -translate-y-1/2 hover:opacity-70 transition-opacity"
                    style={{ color: 'var(--tg-hint)' }}
                  >
                    <svg
                      className="w-4 h-4"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M6 18L18 6M6 6l12 12"
                      />
                    </svg>
                  </button>
                )}
              </div>
            </div>

            {/* Список монет */}
            <div className="max-h-96 overflow-auto">
              {displayCoins.length === 0 ? (
                <div className="p-4 text-center" style={{ color: 'var(--tg-text-tertiary)' }}>
                  {searchQuery ? "Монеты не найдены" : "Нет доступных монет"}
                </div>
              ) : (
                displayCoins.map((coin) => (
                  <button
                    key={coin}
                    onClick={() => handleSelectCoin(coin)}
                    className="w-full px-4 py-3 text-left transition-all flex items-center justify-between"
                    style={{
                      backgroundColor: selectedCoin === coin ? 'rgba(0, 136, 204, 0.15)' : 'transparent',
                    }}
                  >
                    <span
                      className="font-medium"
                      style={{
                        color: selectedCoin === coin ? 'var(--tg-link)' : 'var(--tg-text)',
                      }}
                    >
                      {coin}
                    </span>
                    {selectedCoin === coin && (
                      <svg
                        className="w-5 h-5"
                        fill="currentColor"
                        viewBox="0 0 20 20"
                        style={{ color: 'var(--tg-link)' }}
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                    )}
                  </button>
                ))
              )}
            </div>

            {/* Инфо о количестве */}
            {filteredCoins.length > 100 && (
              <div className="px-4 py-2 text-xs text-center" style={{ backgroundColor: 'var(--tg-bg-tertiary)', borderTop: '1px solid var(--tg-border)', color: 'var(--tg-text-tertiary)' }}>
                Показано 100 из {filteredCoins.length} монет
                {searchQuery && " (уточните поиск)"}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};
