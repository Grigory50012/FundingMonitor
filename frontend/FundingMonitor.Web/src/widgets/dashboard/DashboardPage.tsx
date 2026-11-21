import { ArbitrageTable } from "../../features/arbitrage/ArbitrageTable";
import { CurrentRatesTable as CurrentDataTable } from "../../features/current-rates/CurrentRatesTable";
import { CompactFilter } from "../../features/filters/CompactFilter";
import { HistoryAprTable as HistoryTable } from "../../features/historical-rates/HistoryAprTable";
import { HistoryChartPanel as HistoryPanel } from "../../features/historical-rates/HistoryChartPanel";
import { Panel } from "../../shared/ui/Panel";
import { Spinner } from "../../shared/ui/Spinner";
import type { ExchangeType } from "../../types";
import { TIME_RANGES } from "../../types/history";
import { EMPTY_ARBITRAGE_FILTERS, parsePercent, useDashboardData } from "./useDashboardData";

export function DashboardPage() {
  const {
    mainFilters,
    setMainFilters,
    arbitrageFilters,
    setArbitrageFilters,
    arbitrageFiltersValid,
    arbitrageAutoRefresh,
    setArbitrageAutoRefresh,
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
          className="mb-2 rounded-2xl px-4 py-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"
          style={{
            backgroundColor: "var(--tg-bg-secondary)",
            border: "1px solid var(--tg-border)",
          }}
        >
          <div className="flex flex-wrap items-center gap-2 min-w-0">
            <h2
              className="text-xs font-semibold whitespace-nowrap"
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
        </div>

        <div className="grid grid-cols-1 gap-3 h-[calc(100vh-180px)] min-h-[600px] lg:[grid-template-columns:minmax(0,40%)_minmax(0,1fr)]">
          <Panel className="p-4 flex flex-col min-w-0">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-3">
                <h2 className="panel-title">
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
            <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
              <div className="relative flex flex-wrap items-center gap-x-3 gap-y-2 min-w-0">
                <h2 className="panel-title whitespace-nowrap">
                  {"История"}
                </h2>
                {historyViewMode === "chart" && (
                  <div className="flex flex-wrap items-center gap-1" role="group" aria-label="Период истории">
                    {TIME_RANGES.map((range) => (
                      <button
                        key={range.value}
                        type="button"
                        onClick={() => setTimeRange(range.value)}
                        aria-pressed={timeRange === range.value}
                        className="px-2 py-1 rounded-md text-xs font-medium whitespace-nowrap transition-all"
                        style={{
                          backgroundColor: timeRange === range.value ? "var(--tg-button)" : "var(--tg-bg-tertiary)",
                          color: timeRange === range.value ? "var(--tg-button-text)" : "var(--tg-text-secondary)",
                        }}
                      >
                        {range.label}
                      </button>
                    ))}
                  </div>
                )}
                {historyViewMode === "table" && (
                  <>
                    <span
                      className="text-xs truncate"
                      style={{ color: "var(--tg-text-secondary)" }}
                    >
                      {" / APR по периодам"}
                    </span>
                    <div className="group">
                      <button
                        type="button"
                        aria-label="Пояснения к таблице истории APR"
                        aria-describedby="history-apr-table-help"
                        className="flex size-5 items-center justify-center rounded-full border border-[var(--tg-border)] text-xs font-semibold text-[var(--tg-text-secondary)] hover:text-[var(--tg-text)] focus-visible:outline focus-visible:outline-[var(--tg-button)]"
                      >
                        ?
                      </button>
                      <div
                        id="history-apr-table-help"
                        role="tooltip"
                        className="pointer-events-none absolute top-full left-0 z-[60] w-80 max-w-[calc(100vw-4rem)] pt-1 opacity-0 transition-opacity group-hover:pointer-events-auto group-hover:opacity-100 group-focus-within:pointer-events-auto group-focus-within:opacity-100"
                      >
                        <div className="space-y-1 rounded-xl border border-[var(--tg-border)] bg-[var(--tg-bg-secondary)] p-3 panel-text leading-relaxed text-[var(--tg-text)] shadow-lg">
                          <p><strong className="text-[var(--tg-positive)]">APR</strong> — годовой процент на основе суммарной ставки за период.</p>
                          <p><strong>∑</strong> — суммарная ставка за период.</p>
                          <p><strong>Число</strong> — количество выплат.</p>
                          <p><strong>%</strong> — средняя ставка за выплату.</p>
                        </div>
                      </div>
                    </div>
                  </>
                )}
              </div>
              <div
                className="ml-auto flex items-center gap-1 rounded-lg p-0.5 flex-shrink-0"
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

        <Panel className="mt-3 p-6 flex flex-col h-[650px]">
          <div className="mb-4 flex flex-wrap items-start gap-2 text-xs">
            <div className="relative flex min-h-7 items-center gap-2">
              <h2 className="panel-title">Арбитражные возможности</h2>
              <div className="group">
                <button
                  type="button"
                  aria-label="Пояснения к таблице арбитража"
                  aria-describedby="arbitrage-table-help"
                  className="flex size-5 items-center justify-center rounded-full border border-[var(--tg-border)] text-xs font-semibold text-[var(--tg-text-secondary)] hover:text-[var(--tg-text)] focus-visible:outline focus-visible:outline-[var(--tg-button)]"
                >
                  ?
                </button>
                <div
                  id="arbitrage-table-help"
                  role="tooltip"
                  className="pointer-events-none absolute top-full left-0 z-[60] w-80 max-w-[calc(100vw-4rem)] pt-1 opacity-0 transition-opacity group-hover:pointer-events-auto group-hover:opacity-100 group-focus-within:pointer-events-auto group-focus-within:opacity-100"
                >
                  <div className="space-y-1 rounded-xl border border-[var(--tg-border)] bg-[var(--tg-bg-secondary)] p-3 panel-text leading-relaxed text-[var(--tg-text)] shadow-lg">
                    <p><strong className="text-[var(--tg-positive)]">Long</strong> — биржа для лонга.</p>
                    <p><strong className="text-[var(--tg-negative)]">Short</strong> — биржа для шорта.</p>
                    <p><strong>Спред фандинга</strong> — абсолютная разница ставок за начисление. APR учитывает частоту начислений.</p>
                    <p><strong>Спред цены</strong> — разница цен маркировки Short и Long в процентах и долларах.</p>
                  </div>
                </div>
              </div>
            </div>
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
            {([
              ["minAprPercent", "Минимальный APR пары, %", "Мин. APR, %"],
              ["maxAdversePriceSpreadPercent", "Макс. неблагоприятный спред цены, %", "Макс. спред, %"],
            ] as const).map(([field, label, placeholder]) => {
              const value = arbitrageFilters[field];
              const invalid = value.trim() !== "" && parsePercent(value) === undefined;
              return (
                <label key={field} className="flex flex-col gap-1" title={label}>
                  <span className="sr-only">{label}</span>
                  <span className={`dashboard-control has-[[aria-invalid=true]]:border-[var(--tg-negative)] ${field === "minAprPercent" ? "w-24" : "w-32"}`}>
                    <input
                      className="h-full min-w-0 flex-1 bg-transparent outline-none placeholder:text-[var(--tg-hint)]"
                      inputMode="decimal"
                      placeholder={placeholder}
                      value={value}
                      aria-invalid={invalid}
                      aria-describedby={invalid ? `${field}-error` : undefined}
                      onChange={(event) => setArbitrageFilters({
                        ...arbitrageFilters,
                        [field]: event.target.value,
                      })}
                    />
                    {value !== "" && <span aria-hidden="true" className="text-xs text-[var(--tg-hint)]">%</span>}
                  </span>
                  {invalid && (
                    <span id={`${field}-error`} className="px-1 text-xs text-[var(--tg-negative)]">
                      Введите число от 0
                    </span>
                  )}
                </label>
              );
            })}
            <label
              className="dashboard-control relative cursor-pointer"
              title="Только одинаковые интервалы начисления"
            >
              <input
                className="peer sr-only"
                type="checkbox"
                role="switch"
                checked={arbitrageFilters.sameFundingIntervalOnly}
                onChange={(event) => setArbitrageFilters({
                  ...arbitrageFilters,
                  sameFundingIntervalOnly: event.target.checked,
                })}
              />
              <span aria-hidden="true" className="flex h-4 w-7 shrink-0 items-center rounded-full bg-[var(--tg-hint)] p-0.5 transition-colors peer-checked:bg-[var(--tg-button)] peer-checked:[&>span]:translate-x-3">
                <span className="size-3 rounded-full bg-white transition-transform" />
              </span>
              <span>Равные интервалы</span>
            </label>
            <div className="ml-auto flex shrink-0 items-center gap-2">
              <button
                type="button"
                className="dashboard-control text-[var(--tg-text-secondary)] hover:text-[var(--tg-text)]"
                onClick={() => setArbitrageFilters(EMPTY_ARBITRAGE_FILTERS)}
              >
                Сбросить
              </button>
              <label className="dashboard-control cursor-pointer" title="Обновлять арбитражные возможности каждые 5 секунд">
                <input
                  className="peer sr-only"
                  type="checkbox"
                  role="switch"
                  checked={arbitrageAutoRefresh}
                  onChange={(event) => setArbitrageAutoRefresh(event.target.checked)}
                />
                <span aria-hidden="true" className="flex h-4 w-7 shrink-0 items-center rounded-full bg-[var(--tg-hint)] p-0.5 transition-colors peer-checked:bg-[var(--tg-button)] peer-checked:[&>span]:translate-x-3">
                  <span className="size-3 rounded-full bg-white transition-transform" />
                </span>
                <span>Автообновление</span>
              </label>
            </div>
          </div>
          <div className="flex-1 min-h-0" hidden={!arbitrageFiltersValid}>
              <ArbitrageTable
                data={arbitrage.data}
                filtersKey={arbitrage.filtersKey}
                isLoading={arbitrage.isLoading}
                onArbitrageClick={(symbol, exchanges) =>
                  setMainFilters({
                    symbol,
                    exchanges: exchanges as ExchangeType[],
                  })
                }
              />
          </div>
        </Panel>
      </main>
    </div>
  );
}
