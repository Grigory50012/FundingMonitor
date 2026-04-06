import { useState, useEffect, useCallback } from "react";
import {
  CoinSelector,
  ExchangeSelector,
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

function App() {
  const [selectedCoin, setSelectedCoin] = useState<string>("BTC");
  const [selectedExchanges, setSelectedExchanges] = useState<ExchangeType[]>(
    [],
  );
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
    if (!selectedCoin) return;

    setIsLoadingCurrent(true);
    setError(null);

    try {
      const data = await fundingRatesApi.getCurrentRates({
        symbol: selectedCoin,
        exchanges: selectedExchanges.length > 0 ? selectedExchanges : undefined,
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
  }, [selectedCoin, selectedExchanges]);

  // Загрузка исторических данных
  const loadHistoryData = useCallback(async () => {
    if (!selectedCoin) return;

    setIsLoadingHistory(true);
    setError(null);

    try {
      // Для истории нужен полный символ с -USDT
      const historySymbol = selectedCoin.includes("-")
        ? selectedCoin
        : `${selectedCoin}-USDT`;

      const data = await fundingRatesApi.getHistory({
        symbol: historySymbol,
        exchanges: selectedExchanges.length > 0 ? selectedExchanges : undefined,
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
  }, [selectedCoin, selectedExchanges]);

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
      const data = await fundingRatesApi.getArbitrageSortedByApr({
        symbol: selectedCoin,
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
  }, [selectedCoin]);

  // Загрузка данных при изменении параметров
  useEffect(() => {
    loadCurrentData();
  }, [loadCurrentData]);

  useEffect(() => {
    loadHistoryData();
  }, [loadHistoryData]);

  useEffect(() => {
    loadArbitrageData();
  }, [loadArbitrageData]);

  // Загрузка всех монет при старте
  useEffect(() => {
    loadAllCoins();
  }, [loadAllCoins]);

  // Автообновление текущих данных каждые 30 секунд
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
    <div className="min-h-screen bg-gray-900 text-white">
      {/* Header */}
      <header className="bg-gray-800 border-b border-gray-700 px-6 py-4">
        <div className="max-w-[1920px] mx-auto">
          <h1 className="text-2xl font-bold text-white mb-4">
            Funding Monitor Dashboard
          </h1>

          {/* Панель управления */}
          <div className="flex flex-wrap items-center gap-6">
            <CoinSelector
              selectedCoin={selectedCoin}
              onCoinChange={setSelectedCoin}
              availableCoins={availableCoins}
            />

            <ExchangeSelector
              selectedExchanges={selectedExchanges}
              onExchangesChange={setSelectedExchanges}
            />

            <div className="flex items-center gap-4 ml-auto">
              <button
                onClick={() => {
                  loadCurrentData();
                  loadHistoryData();
                  loadAllCoins();
                }}
                disabled={isLoadingCurrent || isLoadingHistory}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-800
                           disabled:cursor-not-allowed rounded-lg font-medium transition-colors
                           flex items-center gap-2"
              >
                <svg
                  className={`w-4 h-4 ${isLoadingCurrent || isLoadingHistory ? "animate-spin" : ""}`}
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
                Обновить
              </button>

              <div className="flex items-center gap-2 text-sm text-gray-400">
                <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
                Автообновление: 30с
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Основной контент */}
      <main className="max-w-[1920px] mx-auto p-6">
        {error && (
          <div className="mb-6 p-4 bg-red-900/30 border border-red-700 rounded-lg text-red-400">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 h-[calc(100vh-220px)] min-h-[600px]">
          {/* Левая панель - Текущие данные */}
          <div className="bg-gray-800/50 rounded-2xl border border-gray-700 p-6 overflow-hidden flex flex-col">
            <h2 className="text-lg font-semibold text-white mb-4">
              Текущие ставки
            </h2>
            <div className="flex-1 min-h-0 overflow-hidden">
              {isLoadingCurrent ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
                    <p className="text-gray-400">Загрузка текущих данных...</p>
                  </div>
                </div>
              ) : (
                <CurrentDataTable
                  data={currentData}
                  selectedExchanges={selectedExchanges}
                />
              )}
            </div>
          </div>

          {/* Правая панель - История */}
          <div className="bg-gray-800/50 rounded-2xl border border-gray-700 p-6 overflow-hidden flex flex-col">
            {/* Переключатель режима просмотра */}
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <h2 className="text-lg font-semibold text-white">История</h2>
                {historyViewMode === "table" && (
                  <span className="text-sm text-gray-400">
                    / APR по периодам
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2 bg-gray-800 rounded-lg p-1">
                <button
                  onClick={() => setHistoryViewMode("chart")}
                  className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                    historyViewMode === "chart"
                      ? "bg-blue-600 text-white"
                      : "text-gray-400 hover:text-white hover:bg-gray-700"
                  }`}
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
                  className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                    historyViewMode === "table"
                      ? "bg-blue-600 text-white"
                      : "text-gray-400 hover:text-white hover:bg-gray-700"
                  }`}
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

            {/* Контент истории */}
            <div className="flex-1 min-h-0">
              {isLoadingHistory ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
                    <p className="text-gray-400">Загрузка истории...</p>
                  </div>
                </div>
              ) : historyViewMode === "chart" ? (
                <HistoryPanel
                  data={historyData}
                  selectedExchanges={selectedExchanges}
                  timeRange={timeRange}
                  onTimeRangeChange={setTimeRange}
                />
              ) : (
                <HistoryTable
                  symbol={selectedCoin}
                  selectedExchanges={selectedExchanges}
                />
              )}
            </div>
          </div>
        </div>

        {/* Арбитражные возможности — на всю ширину */}
        <div className="mt-6 bg-gray-800/50 rounded-2xl border border-gray-700 p-6 overflow-hidden flex flex-col h-[500px]">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-white flex items-center gap-2">
              <span className="text-green-400">🔥</span>
              Арбитражные возможности
            </h2>
            <button
              onClick={loadArbitrageData}
              disabled={isLoadingArbitrage}
              className="px-3 py-1.5 text-sm bg-blue-600 hover:bg-blue-700 disabled:bg-blue-800 disabled:cursor-not-allowed rounded-lg font-medium transition-colors flex items-center gap-1.5"
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
              Обновить
            </button>
          </div>
          <div className="flex-1 min-h-0">
            {isLoadingArbitrage ? (
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <div className="w-12 h-12 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
                  <p className="text-gray-400">
                    Загрузка арбитражных данных...
                  </p>
                </div>
              </div>
            ) : (
              <ArbitrageTable
                data={arbitrageData}
                onArbitrageClick={(symbol, exchanges) => {
                  setSelectedCoin(symbol);
                  setSelectedExchanges(exchanges as ExchangeType[]);
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
