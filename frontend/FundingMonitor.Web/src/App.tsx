import { useState, useEffect, useCallback } from "react";
import {
  CompactFilter,
  CurrentDataTable,
  HistoryPanel,
  HistoryTable,
  ArbitrageTable,
} from "./components";
import type { TimeRangeType } from "./components";
import { fundingRatesApi } from "./api/fundingRates";
import type {
  FundingRateDto,
  HistoricalFundingRateDto,
  ExchangeType,
  FundingArbitrageDto,
} from "./types";

const DEFAULT_COINS = ["BTC", "ETH", "SOL", "XRP", "DOGE"];

type HistoryViewMode = "chart" | "table";

interface FilterState {
  exchanges: ExchangeType[];
  symbol: string;
}

const STORAGE_KEYS = {
  mainFilters: "fundingMonitor.mainFilters",
  arbitrageFilters: "fundingMonitor.arbitrageFilters",
};

function App() {
  const [mainFilters, setMainFilters] = useState<FilterState>(() => {
    const saved = localStorage.getItem(STORAGE_KEYS.mainFilters);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return { exchanges: [], symbol: "BTC" };
      }
    }
    return { exchanges: [], symbol: "BTC" };
  });

  const [arbitrageFilters, setArbitrageFilters] = useState<FilterState>(() => {
    const saved = localStorage.getItem(STORAGE_KEYS.arbitrageFilters);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // Default to empty symbol (show all arbitrage opportunities by default)
        return { exchanges: [], symbol: "" };
      }
    }
    // Default to empty symbol (show all arbitrage opportunities by default)
    return { exchanges: [], symbol: "" };
  });

  const [currentData, setCurrentData] = useState<FundingRateDto[]>([]);
  const [historyData, setHistoryData] = useState<HistoricalFundingRateDto[]>(
    [],
  );
  const [allCoins, setAllCoins] = useState<string[]>([]);
  const [isLoadingCurrent, setIsLoadingCurrent] = useState(false);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [isLoadingArbitrage, setIsLoadingArbitrage] = useState(false);
  const [arbitrageData, setArbitrageData] = useState<FundingArbitrageDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [historyViewMode, setHistoryViewMode] =
    useState<HistoryViewMode>("chart");
  const [timeRange, setTimeRange] = useState<TimeRangeType>("1d");

  // Загрузка текущих данных
  const loadCurrentData = useCallback(async () => {
    if (!mainFilters.symbol) return;

    setIsLoadingCurrent(true);
    setError(null);

    try {
      const data = await fundingRatesApi.getCurrentRates({
        symbol: mainFilters.symbol,
        exchanges:
          mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
      });
      setCurrentData(data);
    } catch (err: any) {
      console.error("Failed to load current data:", err);
      setError(
        err.response?.data?.details ||
          err.message ||
          "Не удалось загрузить текущие данные",
      );
    } finally {
      setIsLoadingCurrent(false);
    }
  }, [mainFilters.symbol, mainFilters.exchanges]);

  // Загрузка исторических данных
  const loadHistoryData = useCallback(async () => {
    if (!mainFilters.symbol) return;

    setIsLoadingHistory(true);
    setError(null);

    try {
      // Для истории нужен полный символ с -USDT
      const historySymbol = mainFilters.symbol.includes("-")
        ? mainFilters.symbol
        : `${mainFilters.symbol}-USDT`;

      const data = await fundingRatesApi.getHistory({
        symbol: historySymbol,
        exchanges:
          mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
        limit: 1000,
      });
      setHistoryData(data);
    } catch (err: any) {
      console.error("Failed to load history data:", err);
      setError(
        err.response?.data?.details ||
          err.message ||
          "Не удалось загрузить исторические данные",
      );
    } finally {
      setIsLoadingHistory(false);
    }
  }, [mainFilters.symbol, mainFilters.exchanges]);

  // Загрузка всех доступных монет
  const loadAllCoins = useCallback(async () => {
    try {
      const data = await fundingRatesApi.getCurrentRates({});
      const coins = Array.from(
        new Set(data.map((item) => item.symbol.replace("-USDT", ""))),
      ).sort();
      setAllCoins(coins);
    } catch (err) {
      console.error("Failed to load all coins:", err);
    }
  }, []);

  // Загрузка арбитражных данных
  const loadArbitrageData = useCallback(async () => {
    setIsLoadingArbitrage(true);
    setError(null);

    try {
      const data = await fundingRatesApi.getArbitrageOpportunities({
        symbol: arbitrageFilters.symbol || undefined,
        exchanges:
          arbitrageFilters.exchanges.length > 0
            ? arbitrageFilters.exchanges
            : undefined,
      });
      setArbitrageData(data);
    } catch (err: any) {
      console.error("Failed to load arbitrage data:", err);
      setError(
        err.response?.data?.details ||
          err.message ||
          "Не удалось загрузить арбитражные данные",
      );
    } finally {
      setIsLoadingArbitrage(false);
    }
  }, [arbitrageFilters.symbol, arbitrageFilters.exchanges]);

  // Сохранение фильтров в localStorage
  useEffect(() => {
    localStorage.setItem(STORAGE_KEYS.mainFilters, JSON.stringify(mainFilters));
  }, [mainFilters]);

  useEffect(() => {
    localStorage.setItem(
      STORAGE_KEYS.arbitrageFilters,
      JSON.stringify(arbitrageFilters),
    );
  }, [arbitrageFilters]);

  useEffect(() => {
    loadCurrentData();
  }, [loadCurrentData]);

  useEffect(() => {
    loadHistoryData();
  }, [loadHistoryData]);

  useEffect(() => {
    loadArbitrageData();
  }, [loadArbitrageData]);

  useEffect(() => {
    loadAllCoins();
  }, [loadAllCoins]);

  useEffect(() => {
    const interval = setInterval(() => {
      loadCurrentData();
      loadAllCoins();
      loadArbitrageData();
    }, 30000);

    return () => clearInterval(interval);
  }, [loadCurrentData, loadAllCoins, loadArbitrageData]);

  // Получаем доступные монеты (дефолтные + все из API)
  const availableCoins = Array.from(
    new Set([...DEFAULT_COINS, ...allCoins]),
  ).sort();

  return (
    <div
      className="min-h-screen text-[var(--tg-text)]"
      style={{ backgroundColor: "var(--tg-bg)" }}
    >
      {/* Основной контент */}
      <main className="max-w-[1920px] mx-auto p-6">
        {error && (
          <div
            className="mb-6 p-4 rounded-xl"
            style={{
              backgroundColor: "rgba(229, 57, 53, 0.15)",
              border: "1px solid rgba(229, 57, 53, 0.3)",
              color: "var(--tg-negative)",
            }}
          >
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 h-[calc(100vh-180px)] min-h-[600px]">
          {/* Левая панель - Текущие данные */}
          <div
            className="rounded-2xl p-6 overflow-hidden flex flex-col"
            style={{
              backgroundColor: "var(--tg-bg-secondary)",
              border: "1px solid var(--tg-border)",
            }}
          >
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <h2 className="text-lg font-semibold">Текущие ставки</h2>
                <CompactFilter
                  selectedExchanges={mainFilters.exchanges}
                  onExchangesChange={(exchanges) =>
                    setMainFilters({ ...mainFilters, exchanges })
                  }
                  selectedSymbol={mainFilters.symbol}
                  onSymbolChange={(symbol) =>
                    setMainFilters({
                      ...mainFilters,
                      symbol: symbol?.trim() || "BTC",
                    })
                  }
                  availableSymbols={availableCoins}
                />
              </div>
              <button
                onClick={() => {
                  loadCurrentData();
                  loadHistoryData();
                }}
                disabled={isLoadingCurrent || isLoadingHistory}
                className="px-3 py-1.5 text-sm rounded-xl font-medium transition-all flex items-center gap-1.5 disabled:cursor-not-allowed"
                style={{
                  backgroundColor:
                    isLoadingCurrent || isLoadingHistory
                      ? "var(--tg-hint)"
                      : "var(--tg-button)",
                  color: "var(--tg-button-text)",
                  opacity: isLoadingCurrent || isLoadingHistory ? 0.6 : 1,
                }}
              >
                <svg
                  className={`w-3.5 h-3.5 ${isLoadingCurrent || isLoadingHistory ? "animate-spin" : ""}`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                  />
                </svg>
              </button>
            </div>

            <div className="flex-1 min-h-0 overflow-hidden mt-4">
              {isLoadingCurrent ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <div
                      className="w-12 h-12 border-4 rounded-full animate-spin mx-auto mb-4"
                      style={{
                        borderColor: "var(--tg-border)",
                        borderTopColor: "var(--tg-button)",
                      }}
                    />
                    <p style={{ color: "var(--tg-text-secondary)" }}>
                      Загрузка текущих данных...
                    </p>
                  </div>
                </div>
              ) : (
                <CurrentDataTable
                  data={currentData}
                  selectedExchanges={mainFilters.exchanges}
                />
              )}
            </div>
          </div>

          {/* Правая панель - История */}
          <div
            className="rounded-2xl p-6 overflow-hidden flex flex-col"
            style={{
              backgroundColor: "var(--tg-bg-secondary)",
              border: "1px solid var(--tg-border)",
            }}
          >
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <h2 className="text-lg font-semibold">История</h2>
                {historyViewMode === "table" && (
                  <span
                    className="text-sm"
                    style={{ color: "var(--tg-text-secondary)" }}
                  >
                    / APR по периодам
                  </span>
                )}
              </div>
              <div
                className="flex items-center gap-2 rounded-xl p-1"
                style={{ backgroundColor: "var(--tg-bg-tertiary)" }}
              >
                <button
                  onClick={() => setHistoryViewMode("chart")}
                  className="px-3 py-1.5 rounded-lg text-sm font-medium transition-all"
                  style={{
                    backgroundColor:
                      historyViewMode === "chart"
                        ? "var(--tg-button)"
                        : "transparent",
                    color:
                      historyViewMode === "chart"
                        ? "var(--tg-button-text)"
                        : "var(--tg-text-secondary)",
                  }}
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
                      d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"
                    />
                  </svg>
                </button>
                <button
                  onClick={() => setHistoryViewMode("table")}
                  className="px-3 py-1.5 rounded-lg text-sm font-medium transition-all"
                  style={{
                    backgroundColor:
                      historyViewMode === "table"
                        ? "var(--tg-button)"
                        : "transparent",
                    color:
                      historyViewMode === "table"
                        ? "var(--tg-button-text)"
                        : "var(--tg-text-secondary)",
                  }}
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
                      d="M3 10h18M3 14h18m-9-4v8m-7 0h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"
                    />
                  </svg>
                </button>
              </div>
            </div>
            <div className="flex-1 min-h-0">
              {isLoadingHistory ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <div
                      className="w-12 h-12 border-4 rounded-full animate-spin mx-auto mb-4"
                      style={{
                        borderColor: "var(--tg-border)",
                        borderTopColor: "var(--tg-button)",
                      }}
                    />
                    <p style={{ color: "var(--tg-text-secondary)" }}>
                      Загрузка истории...
                    </p>
                  </div>
                </div>
              ) : historyViewMode === "chart" ? (
                <HistoryPanel
                  data={historyData}
                  selectedExchanges={mainFilters.exchanges}
                  timeRange={timeRange}
                  onTimeRangeChange={setTimeRange}
                />
              ) : (
                <HistoryTable
                  symbol={mainFilters.symbol}
                  selectedExchanges={mainFilters.exchanges}
                />
              )}
            </div>
          </div>
        </div>

        {/* Арбитражные возможности — на всю ширину */}
        <div
          className="mt-6 rounded-2xl p-6 overflow-hidden flex flex-col h-[500px]"
          style={{
            backgroundColor: "var(--tg-bg-secondary)",
            border: "1px solid var(--tg-border)",
          }}
        >
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-4">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                Арбитражные возможности
              </h2>
              <CompactFilter
                selectedExchanges={arbitrageFilters.exchanges}
                onExchangesChange={(exchanges) =>
                  setArbitrageFilters({ ...arbitrageFilters, exchanges })
                }
                selectedSymbol={arbitrageFilters.symbol}
                onSymbolChange={(symbol) =>
                  setArbitrageFilters({ ...arbitrageFilters, symbol })
                }
                availableSymbols={availableCoins}
              />
            </div>
            <button
              onClick={loadArbitrageData}
              disabled={isLoadingArbitrage}
              className="px-3 py-1.5 text-sm rounded-xl font-medium transition-all flex items-center gap-1.5 disabled:cursor-not-allowed"
              style={{
                backgroundColor: isLoadingArbitrage
                  ? "var(--tg-hint)"
                  : "var(--tg-button)",
                color: "var(--tg-button-text)",
                opacity: isLoadingArbitrage ? 0.6 : 1,
              }}
            >
              <svg
                className={`w-3.5 h-3.5 ${isLoadingArbitrage ? "animate-spin" : ""}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                />
              </svg>
            </button>
          </div>
          <div className="flex-1 min-h-0">
            {isLoadingArbitrage ? (
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <div
                    className="w-12 h-12 border-4 rounded-full animate-spin mx-auto mb-4"
                    style={{
                      borderColor: "var(--tg-border)",
                      borderTopColor: "var(--tg-button)",
                    }}
                  />
                  <p style={{ color: "var(--tg-text-secondary)" }}>
                    Загрузка арбитражных данных...
                  </p>
                </div>
              </div>
            ) : (
              <ArbitrageTable
                data={arbitrageData}
                onArbitrageClick={(symbol, exchanges) => {
                  setArbitrageFilters({
                    symbol,
                    exchanges: exchanges as ExchangeType[],
                  });
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
