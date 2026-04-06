import React, { useState, useMemo } from "react";
import type { FundingArbitrageDto } from "../types";

interface ArbitrageTableProps {
  data: FundingArbitrageDto[];
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

export const ArbitrageTable: React.FC<ArbitrageTableProps> = ({ data }) => {
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "profitabilityPercent",
    direction: "desc",
  });

  const sortedData = useMemo(() => {
    if (!data.length || !sortConfig.direction) return data;

    const { column, direction } = sortConfig;
    const multiplier = direction === "asc" ? 1 : -1;

    return [...data].sort((a, b) => {
      let valueA: number;
      let valueB: number;

      if (column === "fundingRateSpread") {
        valueA =
          a.fundingRateSpread ??
          ((a.fundingRateA ?? 0) - (a.fundingRateB ?? 0)) * 100;
        valueB =
          b.fundingRateSpread ??
          ((b.fundingRateA ?? 0) - (b.fundingRateB ?? 0)) * 100;
      } else if (column === "profitabilityPercent") {
        valueA = Math.abs(a.aprSpread);
        valueB = Math.abs(b.aprSpread);
      } else if (column === "priceSpreadPercent") {
        valueA = a.priceSpreadPercent;
        valueB = b.priceSpreadPercent;
      } else {
        // symbol — лексикографическая сортировка
        return direction === "asc"
          ? a.symbol.localeCompare(b.symbol)
          : b.symbol.localeCompare(a.symbol);
      }

      return (valueA - valueB) * multiplier;
    });
  }, [data, sortConfig]);

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

  const formatExchangeName = (exchange: string): string => {
    return exchange === "Binance"
      ? "Binance"
      : exchange === "Bybit"
        ? "Bybit"
        : exchange === "OKX"
          ? "OKX"
          : exchange;
  };

  const getExchangeColorClass = (exchange: string): string => {
    return exchange === "Binance"
      ? "bg-yellow-900/50 text-yellow-400"
      : exchange === "Bybit"
        ? "bg-orange-900/50 text-orange-400"
        : "bg-gray-700 text-gray-400";
  };

  if (sortedData.length === 0) {
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
            {sortedData.map((item, index) => {
              const isLongA = item.longExchange === item.exchangeA;
              const fundingRateSpread =
                (item.fundingRateSpread ??
                  (item.fundingRateA ?? 0) - (item.fundingRateB ?? 0)) * 100;
              const fundingRateA = (item.fundingRateA ?? 0) * 100;
              const fundingRateB = (item.fundingRateB ?? 0) * 100;
              return (
                <React.Fragment
                  key={`${item.symbol}-${item.exchangeA}-${item.exchangeB}-${index}`}
                >
                  <tr className="border-t border-gray-800 hover:bg-gray-800/50 transition-colors">
                    {/* Пара — на две строки */}
                    <td
                      className="px-4 py-3 text-center border-l border-gray-800/50 align-middle"
                      rowSpan={2}
                    >
                      <span className="text-white font-semibold text-base">
                        {item.symbol.replace("-USDT", "")}
                      </span>
                    </td>
                    {/* Биржа A — верхняя строка */}
                    <td className="px-4 py-2 border-l border-gray-800/50">
                      <div className="flex items-center gap-2">
                        <span
                          className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(formatExchangeName(item.exchangeA))}`}
                        >
                          {formatExchangeName(item.exchangeA)}
                        </span>
                        <span className="text-xs text-gray-500">
                          {isLongA ? "L" : "S"}
                        </span>
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
                      <p className="text-xs text-gray-600">
                        {item.paymentsA} вып./день
                      </p>
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
                  <tr className="border-t border-gray-800/30 hover:bg-gray-800/50 transition-colors">
                    {/* Биржа B — нижняя строка */}
                    <td className="px-4 py-2 border-l border-gray-800/50">
                      <div className="flex items-center gap-2">
                        <span
                          className={`px-2 py-1 rounded-md text-xs font-semibold ${getExchangeColorClass(formatExchangeName(item.exchangeB))}`}
                        >
                          {formatExchangeName(item.exchangeB)}
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
                      <p className="text-xs text-gray-600">
                        {item.paymentsB} вып./день
                      </p>
                    </td>
                  </tr>
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
