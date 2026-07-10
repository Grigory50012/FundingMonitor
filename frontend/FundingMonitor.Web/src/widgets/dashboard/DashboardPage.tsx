import { ArbitrageTable } from "../../features/arbitrage/ArbitrageTable";
import { CurrentRatesTable as CurrentDataTable } from "../../features/current-rates/CurrentRatesTable";
import { CompactFilter } from "../../features/filters/CompactFilter";
import { HistoryAprTable as HistoryTable } from "../../features/history/HistoryAprTable";
import { HistoryChartPanel as HistoryPanel } from "../../features/history/HistoryChartPanel";
import { Panel } from "../../shared/ui/Panel";
import { Spinner } from "../../shared/ui/Spinner";
import type { ExchangeType } from "../../types";
import { useDashboardData } from "./useDashboardData";

export function DashboardPage() {
  const {
    mainFilters,
    setMainFilters,
    arbitrageFilters,
    setArbitrageFilters,
    current,
    history,
    arbitrage,
    availableCoins,
    historyViewMode,
    setHistoryViewMode,
    timeRange,
    setTimeRange,
    errorMessage,
  } = useDashboardData();

  const isMainLoading = current.isLoading || history.isLoading;

  return (
    <div
      className="min-h-screen text-[var(--tg-text)]"
      style={{ backgroundColor: "var(--tg-bg)" }}
    >
      <main className="max-w-[1920px] mx-auto p-6">
        {errorMessage && (
          <div
            className="mb-6 p-4 rounded-xl"
            style={{
              backgroundColor: "rgba(229, 57, 53, 0.15)",
              border: "1px solid rgba(229, 57, 53, 0.3)",
              color: "var(--tg-negative)",
            }}
          >
            {errorMessage}
          </div>
        )}

        <div
          className="mb-4 rounded-2xl p-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
          style={{
            backgroundColor: "var(--tg-bg-secondary)",
            border: "1px solid var(--tg-border)",
          }}
        >
          <div className="flex items-center gap-3 min-w-0">
            <h2
              className="text-sm font-semibold whitespace-nowrap"
              style={{ color: "var(--tg-text-secondary)" }}
            >
              {"Фильтры"}
            </h2>
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
              current.refresh?.();
              history.refresh?.();
            }}
            disabled={isMainLoading}
            className="px-3 py-1.5 text-sm rounded-xl font-medium transition-all flex items-center gap-1.5 disabled:cursor-not-allowed self-start sm:self-auto"
            style={{
              backgroundColor: isMainLoading
                ? "var(--tg-hint)"
                : "var(--tg-button)",
              color: "var(--tg-button-text)",
              opacity: isMainLoading ? 0.6 : 1,
            }}
            title={"Обновить текущие ставки и историю"}
          >
            <svg
              className={`w-3.5 h-3.5 ${isMainLoading ? "animate-spin" : ""}`}
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
            <span>{"Обновить"}</span>
          </button>
        </div>

        <div className="grid grid-cols-1 gap-6 h-[calc(100vh-180px)] min-h-[600px] lg:[grid-template-columns:minmax(0,40%)_minmax(0,1fr)]">
          <Panel className="p-4 flex flex-col min-w-0">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-3">
                <h2 className="text-base font-semibold">
                  {"Текущие ставки"}
                </h2>
              </div>
            </div>

            <div className="flex-1 min-h-0 overflow-hidden mt-2">
              {current.isLoading ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <Spinner className="w-12 h-12 mx-auto mb-4" />
                    <p style={{ color: "var(--tg-text-secondary)" }}>
                      {"Загрузка текущих данных..."}
                    </p>
                  </div>
                </div>
              ) : (
                <CurrentDataTable
                  data={current.data}
                  selectedExchanges={mainFilters.exchanges}
                />
              )}
            </div>
          </Panel>

          <Panel className="p-4 flex flex-col min-w-0">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-3 min-w-0">
                <h2 className="text-base font-semibold whitespace-nowrap">
                  {"История"}
                </h2>
                {historyViewMode === "table" && (
                  <span
                    className="text-xs truncate"
                    style={{ color: "var(--tg-text-secondary)" }}
                  >
                    {" / APR по периодам"}
                  </span>
                )}
              </div>
              <div
                className="flex items-center gap-1 rounded-lg p-0.5 flex-shrink-0"
                style={{ backgroundColor: "var(--tg-bg-tertiary)" }}
              >
                <button
                  onClick={() => setHistoryViewMode("chart")}
                  className="px-2 py-1 rounded-md text-xs font-medium transition-all"
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
                    className="w-3.5 h-3.5"
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
                  className="px-2 py-1 rounded-md text-xs font-medium transition-all"
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
                    className="w-3.5 h-3.5"
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
            <div className="flex-1 min-h-0 mt-2 overflow-hidden">
              {history.isLoading ? (
                <div className="flex items-center justify-center h-full">
                  <div className="text-center">
                    <Spinner className="w-12 h-12 mx-auto mb-4" />
                    <p style={{ color: "var(--tg-text-secondary)" }}>
                      {"Загрузка истории..."}
                    </p>
                  </div>
                </div>
              ) : historyViewMode === "chart" ? (
                <HistoryPanel
                  data={history.data}
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
          </Panel>
        </div>

        <Panel className="mt-6 p-6 flex flex-col h-[500px]">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-4">
              <h2 className="text-lg font-semibold flex items-center gap-2">
                {"Арбитражные возможности"}
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
              onClick={() => arbitrage.refresh?.()}
              disabled={arbitrage.isLoading}
              className="px-3 py-1.5 text-sm rounded-xl font-medium transition-all flex items-center gap-1.5 disabled:cursor-not-allowed"
              style={{
                backgroundColor: arbitrage.isLoading
                  ? "var(--tg-hint)"
                  : "var(--tg-button)",
                color: "var(--tg-button-text)",
                opacity: arbitrage.isLoading ? 0.6 : 1,
              }}
            >
              <svg
                className={`w-3.5 h-3.5 ${
                  arbitrage.isLoading ? "animate-spin" : ""
                }`}
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
            {arbitrage.isLoading ? (
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <Spinner className="w-12 h-12 mx-auto mb-4" />
                  <p style={{ color: "var(--tg-text-secondary)" }}>
                    {"Загрузка арбитражных данных..."}
                  </p>
                </div>
              </div>
            ) : (
              <ArbitrageTable
                data={arbitrage.data}
                onArbitrageClick={(symbol, exchanges) =>
                  setMainFilters({
                    symbol,
                    exchanges: exchanges as ExchangeType[],
                  })
                }
              />
            )}
          </div>
        </Panel>
      </main>
    </div>
  );
}
