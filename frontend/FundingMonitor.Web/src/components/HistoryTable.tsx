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
      } catch (err) {
        const message = err instanceof Error ? err.message : "Ошибка загрузки данных";
        console.error("Failed to load APR stats:", err);
        setError(message);
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
      <div className="flex items-center justify-center h-full" style={{ color: 'var(--tg-text-secondary)' }}>
        <p>Загрузка данных...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-full" style={{ color: 'var(--tg-negative)' }}>
        <p>Ошибка: {error}</p>
      </div>
    );
  }

  if (filteredData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full" style={{ color: 'var(--tg-text-tertiary)' }}>
        <p>Нет исторических данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead className="sticky top-0 z-10" style={{ backgroundColor: 'var(--tg-bg-secondary)' }}>
            <tr>
              <th className="px-4 py-3 text-left font-medium sticky left-0 z-20 min-w-[120px] border-b" style={{ backgroundColor: 'var(--tg-bg-secondary)', color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}>
                Биржа
              </th>
              {PERIODS.map(({ label }) => (
                <th
                  key={label}
                  className="px-4 py-3 text-center font-medium min-w-[160px] border-b cursor-pointer transition-colors"
                  onClick={() => handleSort(label, "apr")}
                  title="Кликните для сортировки бирж по APR"
                  style={{ color: 'var(--tg-text-secondary)', borderColor: 'var(--tg-border)' }}
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
                className="border-t transition-colors"
                style={{ borderColor: 'var(--tg-border)' }}
              >
                <td className="px-4 py-4 font-medium sticky left-0 z-10 border-r" style={{ backgroundColor: 'var(--tg-bg)', color: 'var(--tg-text)', borderColor: 'var(--tg-border)' }}>
                  <span
                    className="px-3 py-1.5 rounded-xl text-sm font-semibold"
                    style={{
                      backgroundColor: exchange === "Binance" ? 'rgba(241, 196, 15, 0.15)' : exchange === "Bybit" ? 'rgba(230, 126, 34, 0.15)' : 'var(--tg-bg-tertiary)',
                      color: exchange === "Binance" ? '#F1C40F' : exchange === "Bybit" ? '#E67E22' : 'var(--tg-text-secondary)',
                    }}
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
                        className="px-4 py-4 text-center border-l"
                        style={{ borderColor: 'var(--tg-border)', color: 'var(--tg-text-tertiary)' }}
                      >
                        —
                      </td>
                    );
                  }

                  return (
                    <td
                      key={label}
                      className="px-4 py-4 text-center border-l"
                      style={{ borderColor: 'var(--tg-border)' }}
                    >
                      <div className="flex flex-col items-center gap-1">
                        <p
                          className="text-lg font-bold"
                          style={{
                            color: stat.apr > 0 ? 'var(--tg-positive)' : stat.apr < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                          }}
                        >
                          {stat.apr.toFixed(2)}%
                        </p>
                        <p className="text-xs" style={{ color: 'var(--tg-text-secondary)' }}>
                          ∑ {stat.totalFundingRatePercent.toFixed(3)}%
                        </p>
                        <div className="flex items-center gap-2 text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
                          <span>{stat.paymentsCount}</span>
                          <span>•</span>
                          <span>{stat.avgFundingRatePercent.toFixed(3)}%</span>
                        </div>
                        <div className="flex items-center gap-1 text-xs" style={{ color: '#BB86FC' }}>
                          <span>σ</span>
                          <span>{(stat.stdDev ?? 0).toFixed(4)}%</span>
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
      <div className="mt-4 pt-4 border-t" style={{ borderColor: 'var(--tg-border)' }}>
        <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>
          <span className="font-medium" style={{ color: 'var(--tg-positive)' }}>APR</span> — годовой
          процент на основе суммарной ставки за период.
          <span className="mx-2">|</span>
          <span style={{ color: 'var(--tg-text-secondary)' }}>∑</span> — суммарная ставка за период.
          <span className="mx-2">|</span>
          <span>число</span> — количество выплат.
          <span className="mx-2">|</span>
          <span>%</span> — средняя ставка за выплату.
          <span className="mx-2">|</span>
          <span style={{ color: '#BB86FC' }}>σ</span> — среднеквадратическое
          отклонение (волатильность).
        </p>
      </div>
    </div>
  );
};
