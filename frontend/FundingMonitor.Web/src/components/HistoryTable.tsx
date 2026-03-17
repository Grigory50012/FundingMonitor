import React, { useState, useMemo } from "react";
import type { HistoricalFundingRateDto, ExchangeType } from "../types";
import { PERIODS } from "../types";

interface HistoryTableProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
}

interface PeriodStats {
  exchange: ExchangeType;
  period: string;
  days: number;
  apr: number;
  totalFundingRate: number;
  paymentsCount: number;
  avgFundingRate: number;
}

type SortColumn = "apr" | "totalFundingRate" | "paymentsCount";
type SortDirection = "asc" | "desc" | null;

interface SortConfig {
  column: SortColumn;
  direction: SortDirection;
  period?: string; // Период для сортировки
}

export const HistoryTable: React.FC<HistoryTableProps> = ({
  data,
  selectedExchanges,
}) => {
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "apr",
    direction: null,
    period: undefined,
  });

  // Фильтруем данные по выбранным биржам
  const filteredData = data.filter(
    (item) =>
      selectedExchanges.length === 0 ||
      selectedExchanges.includes(item.exchange),
  );

  // Вычисляем APR статистику по периодам
  const stats = useMemo(() => {
    if (filteredData.length === 0) return [];

    const exchangeData = new Map<
      ExchangeType,
      { date: string; rate: number; timestamp: number }[]
    >();

    filteredData.forEach((item) => {
      if (!exchangeData.has(item.exchange)) {
        exchangeData.set(item.exchange, []);
      }
      exchangeData.get(item.exchange)!.push({
        date: new Date(item.fundingTime).toLocaleDateString("ru-RU"),
        rate: item.fundingRate,
        timestamp: new Date(item.fundingTime).getTime(),
      });
    });

    const result: PeriodStats[] = [];

    exchangeData.forEach((rates, exchange) => {
      const sortedRates = rates.sort((a, b) => b.timestamp - a.timestamp);
      const uniqueDates = Array.from(
        new Set(sortedRates.map((r) => r.date)),
      ).sort((a, b) => {
        const [dayA, monthA, yearA] = a.split(".").map(Number);
        const [dayB, monthB, yearB] = b.split(".").map(Number);
        return (
          new Date(yearB, monthB - 1, dayB).getTime() -
          new Date(yearA, monthA - 1, dayA).getTime()
        );
      });

      PERIODS.forEach(({ label, days }) => {
        const datesToInclude = uniqueDates.slice(0, days);
        const ratesForPeriod = sortedRates.filter((r) =>
          datesToInclude.includes(r.date),
        );

        if (ratesForPeriod.length === 0) return;

        const totalFundingRate = ratesForPeriod.reduce(
          (sum, r) => sum + r.rate,
          0,
        );
        const apr = totalFundingRate * 100 * (365 / days);
        const avgFundingRate = totalFundingRate / ratesForPeriod.length;

        result.push({
          exchange,
          period: label,
          days,
          apr,
          totalFundingRate,
          paymentsCount: ratesForPeriod.length,
          avgFundingRate,
        });
      });
    });

    return result;
  }, [filteredData]);

  // Получаем уникальные биржи и периоды (всегда в исходном порядке)
  const exchanges = useMemo(
    () => Array.from(new Set(stats.map((s) => s.exchange))),
    [stats],
  );

  // Сортировка бирж (строк) на основе выбранного столбца периода
  const sortedExchanges = useMemo(() => {
    if (!sortConfig.direction || !sortConfig.period) {
      return exchanges;
    }

    const multiplier = sortConfig.direction === "asc" ? 1 : -1;

    return [...exchanges].sort((a, b) => {
      const statsA = stats.find(
        (s) => s.exchange === a && s.period === sortConfig.period,
      );
      const statsB = stats.find(
        (s) => s.exchange === b && s.period === sortConfig.period,
      );

      if (!statsA || !statsB) return 0;

      let valueA: number;
      let valueB: number;

      if (sortConfig.column === "apr") {
        valueA = statsA.apr;
        valueB = statsB.apr;
      } else if (sortConfig.column === "totalFundingRate") {
        valueA = statsA.totalFundingRate;
        valueB = statsB.totalFundingRate;
      } else {
        valueA = statsA.paymentsCount;
        valueB = statsB.paymentsCount;
      }

      return (valueA - valueB) * multiplier;
    });
  }, [exchanges, stats, sortConfig]);

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

  if (stats.length === 0) {
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
                  const stat = stats.find(
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
                          ∑ {(stat.totalFundingRate * 100).toFixed(3)}%
                        </p>
                        <div className="flex items-center gap-2 text-xs text-gray-600">
                          <span>{stat.paymentsCount}</span>
                          <span>•</span>
                          <span>{(stat.avgFundingRate * 100).toFixed(3)}%</span>
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
