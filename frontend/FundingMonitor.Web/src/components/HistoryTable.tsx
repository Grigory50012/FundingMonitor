import React, { useState, useMemo, useEffect } from "react";
import type { AprPeriodStatsDto, ExchangeType } from "../types";
import { fundingRatesApi } from "../api/fundingRates";
import { PERIODS } from "../types";

interface HistoryTableProps {
  selectedExchanges: ExchangeType[];
  symbol: string;
}

interface SortConfig {
  column: SortColumn;
  direction: SortDirection;
  period?: string;
}

type SortColumn =
  | "apr"
  | "totalFundingRatePercent"
  | "paymentsCount"
  | "avgFundingRatePercent";
type SortDirection = "asc" | "desc" | null;

export const HistoryTable: React.FC<HistoryTableProps> = ({
  selectedExchanges,
  symbol,
}) => {
  const [data, setData] = useState<AprPeriodStatsDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "apr",
    direction: null,
    period: undefined,
  });

  // Загрузка данных с бэкенда
  useEffect(() => {
    if (!symbol) {
      setData([]);
      return;
    }

    const loadData = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const stats = await fundingRatesApi.getAprStats({
          symbol,
          exchanges:
            selectedExchanges.length > 0 ? selectedExchanges : undefined,
        });
        setData(stats);
      } catch (err: any) {
        console.error("Failed to load APR stats:", err);
        setError(err.message || "Ошибка загрузки данных");
      } finally {
        setIsLoading(false);
      }
    };

    loadData();
  }, [symbol, selectedExchanges]);

  // Фильтруем данные по выбранным биржам (на случай если фильтр не применился на бэкенде)
  const filteredData = useMemo(() => {
    if (selectedExchanges.length === 0) return data;
    return data.filter((item) => selectedExchanges.includes(item.exchange));
  }, [data, selectedExchanges]);

  // Получаем уникальные биржи и периоды (всегда в исходном порядке)
  const exchanges = useMemo(
    () => Array.from(new Set(filteredData.map((s) => s.exchange))),
    [filteredData],
  );

  // Сортировка бирж (строк) на основе выбранного столбца периода
  const sortedExchanges = useMemo(() => {
    if (!sortConfig.direction || !sortConfig.period) {
      return exchanges;
    }

    const multiplier = sortConfig.direction === "asc" ? 1 : -1;

    return [...exchanges].sort((a, b) => {
      const statsA = filteredData.find(
        (s) => s.exchange === a && s.period === sortConfig.period,
      );
      const statsB = filteredData.find(
        (s) => s.exchange === b && s.period === sortConfig.period,
      );

      if (!statsA || !statsB) return 0;

      let valueA: number;
      let valueB: number;

      if (sortConfig.column === "apr") {
        valueA = statsA.apr;
        valueB = statsB.apr;
      } else if (sortConfig.column === "totalFundingRatePercent") {
        valueA = statsA.totalFundingRatePercent;
        valueB = statsB.totalFundingRatePercent;
      } else if (sortConfig.column === "avgFundingRatePercent") {
        valueA = statsA.avgFundingRatePercent;
        valueB = statsB.avgFundingRatePercent;
      } else {
        valueA = statsA.paymentsCount;
        valueB = statsB.paymentsCount;
      }

      return (valueA - valueB) * multiplier;
    });
  }, [exchanges, filteredData, sortConfig]);

  // Обработчик клика по заголовку столбца периода
  const handleSort = (period: string, column: SortColumn) => {
    setSortConfig((prev) => {
      if (prev.period === period && prev.column === column) {
        if (prev.direction === "asc")
          return { column, direction: "desc", period };
        if (prev.direction === "desc")
          return { column, direction: null, period };
        return { column, direction: "asc", period };
      }
      return { column, direction: "asc", period };
    });
  };

  // Иконка сортировки
  const SortIcon = ({
    period,
    column,
  }: {
    period: string;
    column: SortColumn;
  }) => {
    if (
      sortConfig.period !== period ||
      sortConfig.column !== column ||
      !sortConfig.direction
    ) {
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

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Загрузка данных...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-full text-red-400">
        <p>Ошибка: {error}</p>
      </div>
    );
  }

  if (filteredData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Нет исторических данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead className="bg-gray-800 sticky top-0 z-10">
            <tr>
              <th className="px-4 py-3 text-left text-gray-400 font-medium sticky left-0 bg-gray-800 z-20 min-w-[120px] border-b border-gray-700">
                Биржа
              </th>
              {PERIODS.map(({ label }) => (
                <th
                  key={label}
                  className="px-4 py-3 text-center text-gray-400 font-medium min-w-[160px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                  onClick={() => handleSort(label, "apr")}
                  title="Кликните для сортировки бирж по APR"
                >
                  <div className="flex items-center justify-center gap-2">
                    <span>{label}</span>
                    <SortIcon period={label} column="apr" />
                  </div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sortedExchanges.map((exchange) => (
              <tr
                key={exchange}
                className="border-t border-gray-800 hover:bg-gray-800/50 transition-colors"
              >
                <td className="px-4 py-4 text-white font-medium sticky left-0 bg-gray-900 z-10 border-r border-gray-700/50">
                  <span
                    className={`px-3 py-1.5 rounded-lg text-sm font-semibold ${
                      exchange === "Binance"
                        ? "bg-yellow-900/50 text-yellow-400"
                        : exchange === "Bybit"
                          ? "bg-orange-900/50 text-orange-400"
                          : "bg-gray-700 text-gray-400"
                    }`}
                  >
                    {exchange}
                  </span>
                </td>
                {PERIODS.map(({ label }) => {
                  const stat = filteredData.find(
                    (s) => s.exchange === exchange && s.period === label,
                  );

                  if (!stat) {
                    return (
                      <td
                        key={label}
                        className="px-4 py-4 text-center text-gray-600 border-l border-gray-800/50"
                      >
                        —
                      </td>
                    );
                  }

                  return (
                    <td
                      key={label}
                      className="px-4 py-4 text-center border-l border-gray-800/50"
                    >
                      <div className="flex flex-col items-center gap-1">
                        <p
                          className={`text-lg font-bold ${
                            stat.apr > 0
                              ? "text-green-400"
                              : stat.apr < 0
                                ? "text-red-400"
                                : "text-gray-400"
                          }`}
                        >
                          {stat.apr.toFixed(2)}%
                        </p>
                        <p className="text-xs text-gray-500">
                          ∑ {stat.totalFundingRatePercent.toFixed(3)}%
                        </p>
                        <div className="flex items-center gap-2 text-xs text-gray-600">
                          <span>{stat.paymentsCount}</span>
                          <span>•</span>
                          <span>{stat.avgFundingRatePercent.toFixed(3)}%</span>
                        </div>
                      </div>
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Пояснение */}
      <div className="mt-4 pt-4 border-t border-gray-700">
        <p className="text-xs text-gray-500">
          <span className="text-green-400 font-medium">APR</span> — годовой
          процент на основе суммарной ставки за период.
          <span className="mx-2">|</span>
          <span className="text-gray-400">∑</span> — суммарная ставка за период.
          <span className="mx-2">|</span>
          <span className="text-gray-600">число</span> — количество выплат.
          <span className="mx-2">|</span>
          <span className="text-gray-600">%</span> — средняя ставка за выплату.
        </p>
      </div>
    </div>
  );
};
