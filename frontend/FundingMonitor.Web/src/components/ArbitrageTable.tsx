import React, { useState, useMemo, useCallback } from "react";
import type { FundingArbitrageDto } from "../types";

interface ArbitrageTableProps {
  data: FundingArbitrageDto[];
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void;
}

type SortColumn =
  | "profitabilityPercent"
  | "priceSpreadPercent"
  | "fundingRateSpread"
  | "symbol";
type SortDirection = "asc" | "desc" | null;

interface SortConfig {
  column: SortColumn;
  direction: SortDirection;
}

interface SymbolGroup {
  symbol: string;
  best: FundingArbitrageDto;
  others: FundingArbitrageDto[];
}

// Хелпер для расчёта спрэда фандинга
const calcFundingSpread = (item: FundingArbitrageDto): number => {
  const spread =
    item.fundingRateSpread ??
    (item.fundingRateA ?? 0) - (item.fundingRateB ?? 0);
  return spread * 100;
};

const calcFundingRate = (rate: number): number => rate * 100;

const getProfitability = (item: FundingArbitrageDto): number =>
  Math.abs(item.aprSpread);

const renderRow = (
  item: FundingArbitrageDto,
  key: string,
  showSymbol: boolean,
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void,
): React.ReactNode => {
  const isLongA = item.longExchange === item.exchangeA;
  const fundingRateSpread = calcFundingSpread(item);
  const fundingRateA = calcFundingRate(item.fundingRateA);
  const fundingRateB = calcFundingRate(item.fundingRateB);

  const handleClick = () => {
    if (onArbitrageClick) {
      const symbol = item.symbol.replace("-USDT", "");
      onArbitrageClick(symbol, [item.exchangeA, item.exchangeB]);
    }
  };

  const symbolCell = showSymbol ? (
    <td
      className="px-4 py-3 text-center border-l align-middle"
      rowSpan={2}
      style={{ borderColor: 'var(--tg-border)' }}
    >
      <span className="font-semibold text-base" style={{ color: 'var(--tg-text)' }}>
        {item.symbol.replace("-USDT", "")}
      </span>
    </td>
  ) : (
    <td
      className="px-4 py-3 text-center border-l align-middle"
      rowSpan={2}
      style={{ borderColor: 'var(--tg-border)' }}
    />
  );

  return (
    <React.Fragment key={key}>
      <tr
        className={`border-t transition-colors cursor-pointer`}
        onClick={handleClick}
        style={{ borderColor: 'var(--tg-border)' }}
      >
        {symbolCell}
        {/* Биржа A */}
        <td className="px-4 py-2 border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <div className="flex items-center gap-2">
            <span
              className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(item.exchangeA)}`}
            >
              {item.exchangeA}
            </span>
            <span className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>{isLongA ? "L" : "S"}</span>
          </div>
        </td>
        {/* Цена A */}
        <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
            $
            {item.priceA.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Спред цены — на две строки */}
        <td
          className="px-4 py-3 text-center border-l align-middle"
          rowSpan={2}
          style={{ borderColor: 'var(--tg-border)' }}
        >
          <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
            {item.priceSpreadPercent.toFixed(4)}%
          </p>
          <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
            $
            {item.priceSpread.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 4,
            })}
          </p>
        </td>
        {/* Funding Rate A */}
        <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <p
            className="text-sm font-bold"
            style={{
              color: fundingRateA > 0 ? 'var(--tg-positive)' : fundingRateA < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
            }}
          >
            {fundingRateA.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>{item.paymentsA} вып./день</p>
        </td>
        {/* Спред фандинга — на две строки */}
        <td
          className="px-4 py-3 text-center border-l align-middle"
          rowSpan={2}
          style={{ borderColor: 'var(--tg-border)' }}
        >
          <p
            className="text-sm font-bold"
            style={{
              color: fundingRateSpread > 0 ? 'var(--tg-positive)' : fundingRateSpread < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
            }}
          >
            {fundingRateSpread.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
        </td>
      </tr>
      <tr
        className={`border-t transition-colors cursor-pointer`}
        onClick={handleClick}
        style={{ borderColor: 'var(--tg-border)' }}
      >
        {/* Биржа B */}
        <td className="px-4 py-2 border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <div className="flex items-center gap-2">
            <span
              className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(item.exchangeB)}`}
            >
              {item.exchangeB}
            </span>
            <span className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
              {!isLongA ? "L" : "S"}
            </span>
          </div>
        </td>
        {/* Цена B */}
        <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
            $
            {item.priceB.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Funding Rate B */}
        <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
          <p
            className="text-sm font-bold"
            style={{
              color: fundingRateB > 0 ? 'var(--tg-positive)' : fundingRateB < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
            }}
          >
            {fundingRateB.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>{item.paymentsB} вып./день</p>
        </td>
      </tr>
    </React.Fragment>
  );
};

const getExchangeColorClass = (exchange: string): string => {
  return exchange === "Binance"
    ? "bg-[rgba(241,196,15,0.15)] text-[#F1C40F]"
    : exchange === "Bybit"
      ? "bg-[rgba(230,126,34,0.15)] text-[#E67E22]"
      : "bg-[var(--tg-bg-tertiary)] text-[var(--tg-text-secondary)]";
};

export const ArbitrageTable: React.FC<ArbitrageTableProps> = ({
  data,
  onArbitrageClick,
}) => {
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "profitabilityPercent",
    direction: "desc",
  });
  const [expandedSymbols, setExpandedSymbols] = useState<Set<string>>(
    new Set(),
  );

  const handleSort = (column: SortColumn) => {
    setSortConfig((prev) => {
      if (prev.column === column) {
        if (prev.direction === "asc") return { column, direction: "desc" };
        if (prev.direction === "desc") return { column, direction: null };
        return { column, direction: "asc" };
      }
      return { column, direction: "asc" };
    });
  };

  const toggleExpand = useCallback((symbol: string) => {
    setExpandedSymbols((prev) => {
      const next = new Set(prev);
      if (next.has(symbol)) next.delete(symbol);
      else next.add(symbol);
      return next;
    });
  }, []);

  // Сортировка всех записей
  const sortedData = useMemo(() => {
    if (!data.length || !sortConfig.direction) return data;

    const { column, direction } = sortConfig;
    const multiplier = direction === "asc" ? 1 : -1;

    return [...data].sort((a, b) => {
      let valueA: number;
      let valueB: number;

      if (column === "fundingRateSpread") {
        valueA = calcFundingSpread(a);
        valueB = calcFundingSpread(b);
      } else if (column === "profitabilityPercent") {
        valueA = getProfitability(a);
        valueB = getProfitability(b);
      } else if (column === "priceSpreadPercent") {
        valueA = a.priceSpreadPercent;
        valueB = b.priceSpreadPercent;
      } else {
        return direction === "asc"
          ? a.symbol.localeCompare(b.symbol)
          : b.symbol.localeCompare(a.symbol);
      }

      return (valueA - valueB) * multiplier;
    });
  }, [data, sortConfig]);

  // Группировка по символу (данные уже отсортированы, поэтому лучший — первый)
  const groupedData = useMemo(() => {
    const groups = new Map<string, FundingArbitrageDto[]>();
    for (const item of sortedData) {
      const existing = groups.get(item.symbol);
      if (existing) existing.push(item);
      else groups.set(item.symbol, [item]);
    }

    const result: SymbolGroup[] = [];
    for (const [symbol, items] of groups) {
      result.push({
        symbol,
        best: items[0],
        others: items.slice(1),
      });
    }
    return result;
  }, [sortedData]);

  const SortIcon = ({ column }: { column: SortColumn }) => {
    if (sortConfig.column !== column || !sortConfig.direction) {
      return (
        <svg
          className="w-4 h-4 text-gray-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
          />
        </svg>
      );
    }
    if (sortConfig.direction === "asc") {
      return (
        <svg
          className="w-4 h-4 text-blue-400"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M5 15l7-7 7 7"
          />
        </svg>
      );
    }
    return (
      <svg
        className="w-4 h-4 text-blue-400"
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
    );
  };

  if (groupedData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full" style={{ color: 'var(--tg-text-tertiary)' }}>
        <p>Нет арбитражных возможностей</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead className="sticky top-0 z-10" style={{ backgroundColor: 'var(--tg-bg-secondary)' }}>
            <tr>
              <th
                className="px-4 py-3 text-left font-medium min-w-[100px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("symbol")}
                style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}
              >
                <div className="flex items-center gap-2">
                  <span>Пара</span>
                  <SortIcon column="symbol" />
                </div>
              </th>
              <th className="px-4 py-3 text-center font-medium min-w-[200px] border-b" style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}>
                Биржи
              </th>
              <th className="px-4 py-3 text-center font-medium min-w-[140px] border-b" style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}>
                Цена
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[110px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("priceSpreadPercent")}
                style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Спред цены</span>
                  <SortIcon column="priceSpreadPercent" />
                </div>
              </th>
              <th className="px-4 py-3 text-center font-medium min-w-[120px] border-b" style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}>
                Funding Rate
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[140px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("fundingRateSpread")}
                style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Спред фандинга</span>
                  <SortIcon column="fundingRateSpread" />
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            {groupedData.map((group) => {
              const isExpanded = expandedSymbols.has(group.symbol);
              return (
                <React.Fragment key={group.symbol}>
                  {/* Лучшая связка */}
                  <React.Fragment>
                    <tr
                      className="border-t transition-colors cursor-pointer"
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                      style={{ borderColor: 'var(--tg-border)' }}
                    >
                      {/* Пара + кнопка разворота */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: 'var(--tg-border)' }}
                      >
                        <div className="flex items-center justify-center gap-1">
                          <span className="font-semibold text-base" style={{ color: 'var(--tg-text)' }}>
                            {group.best.symbol.replace("-USDT", "")}
                          </span>
                          {group.others.length > 0 && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                toggleExpand(group.symbol);
                              }}
                              className="p-0.5 rounded transition-colors"
                              style={{ backgroundColor: 'transparent' }}
                            >
                              {isExpanded ? (
                                <svg
                                  className="w-3.5 h-3.5"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                  style={{ color: 'var(--tg-text-secondary)' }}
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M5 15l7-7 7 7"
                                  />
                                </svg>
                              ) : (
                                <svg
                                  className="w-3.5 h-3.5"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                  style={{ color: 'var(--tg-text-secondary)' }}
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M19 9l-7 7-7-7"
                                  />
                                </svg>
                              )}
                            </button>
                          )}
                        </div>
                      </td>
                      {/* Биржа A */}
                      <td className="px-4 py-2 border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <div className="flex items-center gap-2">
                          <span
                            className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(group.best.exchangeA)}`}
                          >
                            {group.best.exchangeA}
                          </span>
                          <span className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                            {group.best.longExchange === group.best.exchangeA
                              ? "L"
                              : "S"}
                          </span>
                        </div>
                      </td>
                      {/* Цена A */}
                      <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
                          $
                          {group.best.priceA.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Спред цены */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: 'var(--tg-border)' }}
                      >
                        <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
                          {group.best.priceSpreadPercent.toFixed(4)}%
                        </p>
                        <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                          $
                          {group.best.priceSpread.toLocaleString(undefined, {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 4,
                          })}
                        </p>
                      </td>
                      {/* Funding Rate A */}
                      <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <p
                          className="text-sm font-bold"
                          style={{
                            color: calcFundingRate(group.best.fundingRateA) > 0 ? 'var(--tg-positive)' : calcFundingRate(group.best.fundingRateA) < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                          }}
                        >
                          {calcFundingRate(
                            group.best.fundingRateA,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                          {group.best.paymentsA} вып./день
                        </p>
                      </td>
                      {/* Спред фандинга */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: 'var(--tg-border)' }}
                      >
                        <p
                          className="text-sm font-bold"
                          style={{
                            color: calcFundingSpread(group.best) > 0 ? 'var(--tg-positive)' : calcFundingSpread(group.best) < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                          }}
                        >
                          {calcFundingSpread(group.best).toLocaleString(
                            undefined,
                            {
                              minimumFractionDigits: 0,
                              maximumFractionDigits: 4,
                            },
                          )}
                          %
                        </p>
                      </td>
                    </tr>
                    <tr
                      className="border-t transition-colors cursor-pointer"
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                      style={{ borderColor: 'var(--tg-border)' }}
                    >
                      {/* Биржа B */}
                      <td className="px-4 py-2 border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <div className="flex items-center gap-2">
                          <span
                            className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(group.best.exchangeB)}`}
                          >
                            {group.best.exchangeB}
                          </span>
                          <span className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                            {group.best.longExchange === group.best.exchangeA
                              ? "S"
                              : "L"}
                          </span>
                        </div>
                      </td>
                      {/* Цена B */}
                      <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <p className="font-semibold text-sm" style={{ color: 'var(--tg-text)' }}>
                          $
                          {group.best.priceB.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Funding Rate B */}
                      <td className="px-4 py-2 text-center border-l" style={{ borderColor: 'var(--tg-border)' }}>
                        <p
                          className="text-sm font-bold"
                          style={{
                            color: calcFundingRate(group.best.fundingRateB) > 0 ? 'var(--tg-positive)' : calcFundingRate(group.best.fundingRateB) < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                          }}
                        >
                          {calcFundingRate(
                            group.best.fundingRateB,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                          {group.best.paymentsB} вып./день
                        </p>
                      </td>
                    </tr>
                  </React.Fragment>

                  {/* Дополнительные связки (при развороте) */}
                  {isExpanded &&
                    group.others.map((item, idx) =>
                      renderRow(
                        item,
                        `${item.symbol}-${item.exchangeA}-${item.exchangeB}-extra-${idx}`,
                        false,
                        onArbitrageClick,
                      ),
                    )}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Пояснение */}
      <div className="mt-4 pt-4 border-t" style={{ borderColor: 'var(--tg-border)' }}>
        <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
          <span className="font-medium" style={{ color: 'var(--tg-positive)' }}>L</span> — биржа для
          лонга (ниже APR).
          <span className="mx-2">|</span>
          <span className="font-medium" style={{ color: 'var(--tg-positive)' }}>S</span> — биржа для
          шорта (выше APR).
          <span className="mx-2">|</span>
          <span className="font-medium" style={{ color: 'var(--tg-positive)' }}>Спред фандинга</span> —
          разница funding rate за период.
          <span className="mx-2">|</span>
          <span className="font-medium" style={{ color: 'var(--tg-text)' }}>Спред цены</span> — разница
          цен между биржами (% и $).
        </p>
      </div>
    </div>
  );
};
