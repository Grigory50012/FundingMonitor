import React, { useMemo } from "react";
import type { ExchangeType } from "../../types";
import { PERIODS } from "../../types";
import type { AprPeriodStatsDto } from "../../types";

interface HistoryTableProps {
  selectedExchanges: ExchangeType[];
  data: AprPeriodStatsDto[];
  isLoading: boolean;
  error: string | null;
  hasData: boolean;
  exchangeOrder: ExchangeType[];
  sortConfig: HistoryAprSortConfig;
  onSortChange: (sort: HistoryAprSortConfig) => void;
}

export interface HistoryAprSortConfig {
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

export const HistoryAprTable: React.FC<HistoryTableProps> = ({
  selectedExchanges,
  data,
  isLoading,
  error,
  hasData,
  exchangeOrder,
  sortConfig,
  onSortChange,
}) => {
  // Фильтруем данные по выбранным биржам (на случай если фильтр не применился на бэкенде)
  const filteredData = useMemo(() => {
    if (selectedExchanges.length === 0) return data;
    return data.filter((item) => selectedExchanges.includes(item.exchange));
  }, [data, selectedExchanges]);

  const sortedExchanges = exchangeOrder;

  const handleSort = (period: string, column: SortColumn) => {
    const direction = sortConfig.period === period && sortConfig.column === column
      ? sortConfig.direction === "asc" ? "desc" : sortConfig.direction === "desc" ? null : "asc"
      : "asc";
    onSortChange({ column, period, direction });
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
          className="w-3 h-3 text-gray-600 flex-shrink-0"
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
          className="w-3 h-3 text-blue-400 flex-shrink-0"
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
        className="w-3 h-3 text-blue-400 flex-shrink-0"
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
      <div
        className="flex items-center justify-center h-full"
        style={{ color: "var(--tg-text-secondary)" }}
      >
        <p className="panel-text">Загрузка данных...</p>
      </div>
    );
  }

  if (error && !hasData) {
    return (
      <div
        className="flex items-center justify-center h-full"
        style={{ color: "var(--tg-negative)" }}
      >
        <p className="panel-text">Ошибка: {error}</p>
      </div>
    );
  }

  if (exchangeOrder.length === 0) {
    return (
      <div
        className="flex items-center justify-center h-full"
        style={{ color: "var(--tg-text-tertiary)" }}
      >
        <div className="text-center panel-text">
          <p>Нет исторических данных для отображения</p>
          {error && <p role="status" className="mt-2 text-[var(--tg-warning)]">Не удалось обновить APR: {error}</p>}
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      <div className="flex-1 min-h-0 overflow-auto [scrollbar-gutter:stable]">
        <table className="funding-rates-table min-w-[720px] w-full table-fixed panel-text border-separate border-spacing-0 [&_tbody_td]:border-b [&_tbody_td]:border-[var(--tg-border)]">
          <colgroup>
            <col className="w-[100px]" />
            {PERIODS.map((p) => (
              <col key={p.label} />
            ))}
          </colgroup>
          <thead style={{ backgroundColor: "var(--tg-bg-secondary)" }}>
            <tr>
              <th
                className="px-1.5 py-1.5 text-center font-medium sticky top-0 left-0 z-30 border-b"
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Биржа
              </th>
              {PERIODS.map(({ label }) => (
                <th
                  key={label}
                  className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
                  onClick={() => handleSort(label, "apr")}
                  title="Кликните для сортировки бирж по APR"
                  style={{
                    position: "sticky",
                    top: 0,
                    backgroundColor: "var(--tg-bg-secondary)",
                    color: "var(--tg-text-secondary)",
                    borderColor: "var(--tg-border)",
                  }}
                >
                  <div className="flex items-center justify-center gap-0.5 flex-wrap">
                    <span className="leading-tight">{label}</span>
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
                style={{ borderColor: "var(--tg-border)" }}
              >
                <td
                  className="px-1.5 py-2 font-medium sticky left-0 z-10 border-r align-middle text-center"
                  style={{
                    backgroundColor: "var(--tg-bg)",
                    color: "var(--tg-text)",
                    borderColor: "var(--tg-border)",
                  }}
                >
                  <div className="flex flex-col items-center justify-center gap-0.5 min-w-0">
                    <span
                      className="px-1.5 py-0.5 rounded-md panel-caption font-semibold mx-auto max-w-full truncate inline-block text-center"
                      style={{
                        backgroundColor:
                          exchange === "Binance"
                            ? "rgba(241, 196, 15, 0.15)"
                            : exchange === "Bybit"
                              ? "rgba(230, 126, 34, 0.15)"
                              : "var(--tg-bg-tertiary)",
                        color:
                          exchange === "Binance"
                            ? "#F1C40F"
                            : exchange === "Bybit"
                              ? "#E67E22"
                              : "var(--tg-text-secondary)",
                      }}
                    >
                      {exchange}
                    </span>
                  </div>
                </td>
                {PERIODS.map(({ label }) => {
                  const stat = filteredData.find(
                    (s) => s.exchange === exchange && s.period === label,
                  );

                  if (!stat) {
                    return (
                      <td
                        key={label}
                        className="px-1 py-2 text-center border-l align-middle"
                        style={{
                          borderColor: "var(--tg-border)",
                          color: "var(--tg-text-tertiary)",
                        }}
                      >
                        —
                      </td>
                    );
                  }

                  return (
                    <td
                      key={label}
                      className="px-1 py-2 text-center border-l align-middle"
                      style={{ borderColor: "var(--tg-border)" }}
                    >
                      <div className="flex flex-col items-center justify-center gap-0.5 leading-tight">
                        <p
                          className="panel-value"
                          style={{
                            color:
                              stat.apr > 0
                                ? "var(--tg-positive)"
                                : stat.apr < 0
                                  ? "var(--tg-negative)"
                                  : "var(--tg-text-tertiary)",
                          }}
                        >
                          {stat.apr.toFixed(2)}%
                        </p>
                        <p
                          className="panel-caption leading-none tabular-nums"
                          style={{ color: "var(--tg-text-secondary)" }}
                        >
                          ∑ {stat.totalFundingRatePercent.toFixed(3)}%
                        </p>
                        <div
                          className="flex items-center gap-1 panel-caption leading-none tabular-nums"
                          style={{ color: "var(--tg-text-tertiary)" }}
                        >
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
      {error && (
        <p role="status" className="mt-2 panel-text text-[var(--tg-warning)]">
          Не удалось обновить APR. Показаны последние полученные данные: {error}
        </p>
      )}
    </div>
  );
};
