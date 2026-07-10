import React, { useEffect, useMemo, useRef, useState } from "react";
import { EXCHANGES } from "../../entities/exchange/model";
import type { ExchangeType } from "../../types";

export interface CompactFilterProps {
  selectedExchanges: ExchangeType[];
  onExchangesChange: (exchanges: ExchangeType[]) => void;
  selectedSymbol: string;
  onSymbolChange: (symbol: string) => void;
  availableSymbols: string[];
}

export const CompactFilter: React.FC<CompactFilterProps> = ({
  selectedExchanges,
  onExchangesChange,
  selectedSymbol,
  onSymbolChange,
  availableSymbols,
}) => {
  const [isSymbolOpen, setIsSymbolOpen] = useState(false);
  const [isExchangeOpen, setIsExchangeOpen] = useState(false);
  const [symbolInput, setSymbolInput] = useState("");

  const symbolRef = useRef<HTMLDivElement>(null);
  const exchangeRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        symbolRef.current &&
        !symbolRef.current.contains(event.target as Node)
      ) {
        setIsSymbolOpen(false);
      }
      if (
        exchangeRef.current &&
        !exchangeRef.current.contains(event.target as Node)
      ) {
        setIsExchangeOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  useEffect(() => {
    setSymbolInput(selectedSymbol ?? "");
  }, [isSymbolOpen, selectedSymbol]);

  const filteredSymbols = useMemo(() => {
    const q = symbolInput.trim().toLowerCase();
    if (!q) return [];

    const base = availableSymbols.filter((symbol) =>
      symbol.toLowerCase().includes(q),
    );

    const rank = (symbol: string) => {
      const target = symbol.toLowerCase();
      if (target === q) return 0;
      if (target.startsWith(q)) return 1;
      return 2;
    };

    return [...base]
      .sort((a, b) => {
        const rankA = rank(a);
        const rankB = rank(b);
        if (rankA !== rankB) return rankA - rankB;
        if (a.length !== b.length) return a.length - b.length;
        return a.localeCompare(b);
      })
      .slice(0, 50);
  }, [availableSymbols, symbolInput]);

  const commitSymbol = (value: string) => {
    onSymbolChange(value.trim());
    setIsSymbolOpen(false);
  };

  const clearSymbol = () => {
    setSymbolInput("");
    onSymbolChange("");
    setIsSymbolOpen(false);
  };

  const toggleExchange = (exchange: ExchangeType) => {
    if (selectedExchanges.includes(exchange)) {
      onExchangesChange(selectedExchanges.filter((item) => item !== exchange));
      return;
    }

    onExchangesChange([...selectedExchanges, exchange]);
  };

  const selectAllExchanges = () => onExchangesChange([...EXCHANGES]);
  const deselectAllExchanges = () => onExchangesChange([]);

  const getExchangeLabel = () => {
    if (selectedExchanges.length === 0) return "Все биржи";
    if (selectedExchanges.length === EXCHANGES.length) return "Все биржи";
    if (selectedExchanges.length <= 2) return selectedExchanges.join(", ");
    return `${selectedExchanges.slice(0, 2).join(", ")} +${
      selectedExchanges.length - 2
    }`;
  };

  return (
    <div className="flex items-center gap-3">
      <div className="relative z-50" ref={symbolRef}>
        <div
          className="h-9 px-3 rounded-xl flex items-center gap-2 transition-all min-w-[220px]"
          style={{
            backgroundColor: "var(--tg-bg-tertiary)",
            border: isSymbolOpen
              ? "1px solid var(--tg-button)"
              : "1px solid var(--tg-border)",
            color: "var(--tg-text)",
          }}
        >
          <input
            type="text"
            value={symbolInput}
            onFocus={() => setIsSymbolOpen(true)}
            onChange={(event) => {
              setSymbolInput(event.target.value);
              setIsSymbolOpen(true);
            }}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                commitSymbol(symbolInput);
              } else if (event.key === "Escape") {
                setIsSymbolOpen(false);
              }
            }}
            placeholder={"Поиск монеты..."}
            className="h-full bg-transparent outline-none text-sm font-medium w-[170px]"
            style={{ color: "var(--tg-text)" }}
          />

          {symbolInput.trim().length > 0 && (
            <button
              type="button"
              onClick={clearSymbol}
              className="ml-auto p-1 rounded-lg transition-colors"
              title={"Сбросить"}
              style={{ color: "var(--tg-hint)" }}
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

        {isSymbolOpen && filteredSymbols.length > 0 && (
          <div
            className="absolute z-50 mt-2 w-64 rounded-2xl shadow-2xl overflow-hidden"
            style={{
              backgroundColor: "var(--tg-bg-secondary)",
              border: "1px solid var(--tg-border)",
            }}
          >
            <div className="p-2">
              {filteredSymbols.map((symbol) => (
                <button
                  key={symbol}
                  onClick={() => {
                    setSymbolInput(symbol);
                    commitSymbol(symbol);
                  }}
                  className="w-full px-3 py-2 text-left transition-all flex items-center justify-between"
                  style={{
                    backgroundColor:
                      symbolInput.trim().toLowerCase() ===
                      symbol.toLowerCase()
                        ? "rgba(0, 136, 204, 0.15)"
                        : "transparent",
                  }}
                >
                  <span
                    className="text-sm font-medium"
                    style={{ color: "var(--tg-text)" }}
                  >
                    {symbol}
                  </span>
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      <div className="relative z-50" ref={exchangeRef}>
        <button
          onClick={() => setIsExchangeOpen(!isExchangeOpen)}
          className="h-9 px-3 rounded-xl flex items-center gap-2 transition-all min-w-[140px]"
          style={{
            backgroundColor: "var(--tg-bg-tertiary)",
            border: isExchangeOpen
              ? "1px solid var(--tg-button)"
              : "1px solid var(--tg-border)",
            color: "var(--tg-text)",
          }}
        >
          <svg
            className="w-4 h-4 flex-shrink-0"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            style={{ color: "var(--tg-hint)" }}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
            />
          </svg>
          <span className="text-sm font-medium truncate">
            {getExchangeLabel()}
          </span>
          <svg
            className={`w-3.5 h-3.5 ml-auto transition-transform ${
              isExchangeOpen ? "rotate-180" : ""
            }`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            style={{ color: "var(--tg-hint)" }}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 9l-7 7-7-7"
            />
          </svg>
        </button>

        {isExchangeOpen && (
          <>
            <div
              className="fixed inset-0 z-40"
              onClick={() => setIsExchangeOpen(false)}
            />
            <div
              className="absolute z-50 mt-2 w-56 rounded-2xl shadow-2xl overflow-hidden"
              style={{
                backgroundColor: "var(--tg-bg-secondary)",
                border: "1px solid var(--tg-border)",
              }}
            >
              <div
                className="p-2"
                style={{ borderBottom: "1px solid var(--tg-border)" }}
              >
                <div className="flex gap-2">
                  <button
                    onClick={selectAllExchanges}
                    className="flex-1 text-xs px-2 py-1.5 rounded-lg transition-colors"
                    style={{ color: "var(--tg-link)" }}
                  >
                    {"Все"}
                  </button>
                  <button
                    onClick={deselectAllExchanges}
                    className="flex-1 text-xs px-2 py-1.5 rounded-lg transition-colors"
                    style={{ color: "var(--tg-text-tertiary)" }}
                  >
                    {"Сброс"}
                  </button>
                </div>
              </div>
              <div className="p-2">
                {EXCHANGES.map((exchange) => (
                  <button
                    key={exchange}
                    onClick={() => toggleExchange(exchange)}
                    className="w-full px-3 py-2 rounded-xl text-left transition-all flex items-center justify-between mb-1 last:mb-0"
                    style={{
                      backgroundColor: selectedExchanges.includes(exchange)
                        ? "rgba(0, 136, 204, 0.15)"
                        : "transparent",
                    }}
                  >
                    <span
                      className="text-sm font-medium"
                      style={{
                        color: selectedExchanges.includes(exchange)
                          ? "var(--tg-link)"
                          : "var(--tg-text)",
                      }}
                    >
                      {exchange}
                    </span>
                    {selectedExchanges.includes(exchange) && (
                      <svg
                        className="w-4 h-4"
                        fill="currentColor"
                        viewBox="0 0 20 20"
                        style={{ color: "var(--tg-link)" }}
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
