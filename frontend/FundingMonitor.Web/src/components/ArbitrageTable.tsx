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
  return (
    item.fundingRateSpread ??
    ((item.fundingRateA ?? 0) - (item.fundingRateB ?? 0)) * 100
  );
};

const calcFundingRate = (rate: number): number => rate * 100;

const getProfitability = (item: FundingArbitrageDto): number =>
  Math.abs(item.aprSpread);

const renderRow = (
  item: FundingArbitrageDto,
  key: string,
  showSymbol: boolean,
  isExpanded?: boolean,
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
      className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
      rowSpan={2}
    >
      <span className="text-white font-semibold text-base">
        {item.symbol.replace("-USDT", "")}
      </span>
    </td>
  ) : (
    <td
      className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
      rowSpan={2}
    />
  );

  return (
    <React.Fragment key={key}>
      <tr
        className={`border-t border-gray-800 transition-colors ${isExpanded ? "bg-gray-800/30" : "hover:bg-gray-800/50"} ${onArbitrageClick ? "cursor-pointer" : ""}`}
        onClick={handleClick}
      >
        {symbolCell}
        {/* Биржа A */}
        <td className="px-4 py-2 border-l border-gray-800/50">
          <div className="flex items-center gap-2">
            <span
              className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(item.exchangeA)}`}
            >
              {item.exchangeA}
            </span>
            <span className="text-xs text-gray-500">{isLongA ? "L" : "S"}</span>
          </div>
        </td>
        {/* Цена A */}
        <td className="px-4 py-2 text-center border-l border-gray-800/50">
          <p className="text-white font-semibold text-sm">
            $
            {item.priceA.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Спред цены — на две строки */}
        <td
          className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
          rowSpan={2}
        >
          <p className="text-white font-semibold text-sm">
            {item.priceSpreadPercent.toFixed(4)}%
          </p>
          <p className="text-gray-500 text-xs">
            $
            {item.priceSpread.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 4,
            })}
          </p>
        </td>
        {/* Funding Rate A */}
        <td className="px-4 py-2 text-center border-l border-gray-800/50">
          <p
            className={`text-sm font-bold ${fundingRateA > 0 ? "text-green-400" : fundingRateA < 0 ? "text-red-400" : "text-gray-400"}`}
          >
            {fundingRateA.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="text-xs text-gray-600">{item.paymentsA} вып./день</p>
        </td>
        {/* Спред фандинга — на две строки */}
        <td
          className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
          rowSpan={2}
        >
          <p
            className={`text-sm font-bold ${fundingRateSpread > 0 ? "text-green-400" : fundingRateSpread < 0 ? "text-red-400" : "text-gray-400"}`}
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
        className={`border-t border-gray-800/30 transition-colors ${isExpanded ? "bg-gray-800/30" : "hover:bg-gray-800/50"} ${onArbitrageClick ? "cursor-pointer" : ""}`}
        onClick={handleClick}
      >
        {/* Биржа B */}
        <td className="px-4 py-2 border-l border-gray-800/50">
          <div className="flex items-center gap-2">
            <span
              className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(item.exchangeB)}`}
            >
              {item.exchangeB}
            </span>
            <span className="text-xs text-gray-500">
              {!isLongA ? "L" : "S"}
            </span>
          </div>
        </td>
        {/* Цена B */}
        <td className="px-4 py-2 text-center border-l border-gray-800/50">
          <p className="text-white font-semibold text-sm">
            $
            {item.priceB.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Funding Rate B */}
        <td className="px-4 py-2 text-center border-l border-gray-800/50">
          <p
            className={`text-sm font-bold ${fundingRateB > 0 ? "text-green-400" : fundingRateB < 0 ? "text-red-400" : "text-gray-400"}`}
          >
            {fundingRateB.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="text-xs text-gray-600">{item.paymentsB} вып./день</p>
        </td>
      </tr>
    </React.Fragment>
  );
};

const getExchangeColorClass = (exchange: string): string => {
  return exchange === "Binance"
    ? "bg-yellow-900/50 text-yellow-400"
    : exchange === "Bybit"
      ? "bg-orange-900/50 text-orange-400"
      : "bg-gray-700 text-gray-400";
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
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Нет арбитражных возможностей</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead className="bg-gray-800 sticky top-0 z-10">
            <tr>
              <th
                className="px-4 py-3 text-left text-gray-400 font-medium min-w-[100px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("symbol")}
              >
                <div className="flex items-center gap-2">
                  <span>Пара</span>
                  <SortIcon column="symbol" />
                </div>
              </th>
              <th className="px-4 py-3 text-center text-gray-400 font-medium min-w-[200px] border-b border-gray-700">
                Биржи
              </th>
              <th className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700">
                Цена
              </th>
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[110px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("priceSpreadPercent")}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Спред цены</span>
                  <SortIcon column="priceSpreadPercent" />
                </div>
              </th>
              <th className="px-4 py-3 text-center text-gray-400 font-medium min-w-[120px] border-b border-gray-700">
                Funding Rate
              </th>
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("fundingRateSpread")}
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
                      className={`border-t border-gray-800 hover:bg-gray-800/50 transition-colors ${onArbitrageClick ? "cursor-pointer" : ""}`}
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                    >
                      {/* Пара + кнопка разворота */}
                      <td
                        className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
                        rowSpan={2}
                      >
                        <div className="flex items-center justify-center gap-1">
                          <span className="text-white font-semibold text-base">
                            {group.best.symbol.replace("-USDT", "")}
                          </span>
                          {group.others.length > 0 && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                toggleExpand(group.symbol);
                              }}
                              className="p-0.5 rounded hover:bg-gray-700 transition-colors"
                            >
                              {isExpanded ? (
                                <svg
                                  className="w-3.5 h-3.5 text-gray-400"
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
                              ) : (
                                <svg
                                  className="w-3.5 h-3.5 text-gray-400"
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
                              )}
                            </button>
                          )}
                        </div>
                      </td>
                      {/* Биржа A */}
                      <td className="px-4 py-2 border-l border-gray-800/50">
                        <div className="flex items-center gap-2">
                          <span
                            className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(group.best.exchangeA)}`}
                          >
                            {group.best.exchangeA}
                          </span>
                          <span className="text-xs text-gray-500">
                            {group.best.longExchange === group.best.exchangeA
                              ? "L"
                              : "S"}
                          </span>
                        </div>
                      </td>
                      {/* Цена A */}
                      <td className="px-4 py-2 text-center border-l border-gray-800/50">
                        <p className="text-white font-semibold text-sm">
                          $
                          {group.best.priceA.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Спред цены */}
                      <td
                        className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
                        rowSpan={2}
                      >
                        <p className="text-white font-semibold text-sm">
                          {group.best.priceSpreadPercent.toFixed(4)}%
                        </p>
                        <p className="text-gray-500 text-xs">
                          $
                          {group.best.priceSpread.toLocaleString(undefined, {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 4,
                          })}
                        </p>
                      </td>
                      {/* Funding Rate A */}
                      <td className="px-4 py-2 text-center border-l border-gray-800/50">
                        <p
                          className={`text-sm font-bold ${calcFundingRate(group.best.fundingRateA) > 0 ? "text-green-400" : calcFundingRate(group.best.fundingRateA) < 0 ? "text-red-400" : "text-gray-400"}`}
                        >
                          {calcFundingRate(
                            group.best.fundingRateA,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p className="text-xs text-gray-600">
                          {group.best.paymentsA} вып./день
                        </p>
                      </td>
                      {/* Спред фандинга */}
                      <td
                        className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
                        rowSpan={2}
                      >
                        <p
                          className={`text-sm font-bold ${calcFundingSpread(group.best) > 0 ? "text-green-400" : calcFundingSpread(group.best) < 0 ? "text-red-400" : "text-gray-400"}`}
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
                      className={`border-t border-gray-800/30 hover:bg-gray-800/50 transition-colors ${onArbitrageClick ? "cursor-pointer" : ""}`}
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                    >
                      {/* Биржа B */}
                      <td className="px-4 py-2 border-l border-gray-800/50">
                        <div className="flex items-center gap-2">
                          <span
                            className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(group.best.exchangeB)}`}
                          >
                            {group.best.exchangeB}
                          </span>
                          <span className="text-xs text-gray-500">
                            {group.best.longExchange === group.best.exchangeA
                              ? "S"
                              : "L"}
                          </span>
                        </div>
                      </td>
                      {/* Цена B */}
                      <td className="px-4 py-2 text-center border-l border-gray-800/50">
                        <p className="text-white font-semibold text-sm">
                          $
                          {group.best.priceB.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Funding Rate B */}
                      <td className="px-4 py-2 text-center border-l border-gray-800/50">
                        <p
                          className={`text-sm font-bold ${calcFundingRate(group.best.fundingRateB) > 0 ? "text-green-400" : calcFundingRate(group.best.fundingRateB) < 0 ? "text-red-400" : "text-gray-400"}`}
                        >
                          {calcFundingRate(
                            group.best.fundingRateB,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p className="text-xs text-gray-600">
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
                        true,
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
      <div className="mt-4 pt-4 border-t border-gray-700">
        <p className="text-xs text-gray-500">
          <span className="text-green-400 font-medium">L</span> — биржа для
          лонга (ниже APR).
          <span className="mx-2">|</span>
          <span className="text-green-400 font-medium">S</span> — биржа для
          шорта (выше APR).
          <span className="mx-2">|</span>
          <span className="text-green-400 font-medium">Спред фандинга</span> —
          разница funding rate за период.
          <span className="mx-2">|</span>
          <span className="text-white font-medium">Спред цены</span> — разница
          цен между биржами (% и $).
        </p>
      </div>
    </div>
  );
};
