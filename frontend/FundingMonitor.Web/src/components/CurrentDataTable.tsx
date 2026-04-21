import React, { useState, useMemo } from "react";
import type { FundingRateDto, ExchangeType } from "../types";

interface CurrentDataTableProps {
  data: FundingRateDto[];
  selectedExchanges: ExchangeType[];
}

type SortColumn = "fundingRate" | "apr" | "markPrice" | "nextFundingTime";
type SortDirection = "asc" | "desc" | null;

interface SortConfig {
  column: SortColumn;
  direction: SortDirection;
}

export const CurrentDataTable: React.FC<CurrentDataTableProps> = ({
  data,
  selectedExchanges,
}) => {
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "fundingRate",
    direction: null,
  });

  // Unified formatting helpers
  const formatFixed = (value: number, digits: number): string => {
    return value.toLocaleString(undefined, {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
    });
  };
  const formatFundingPct = (value: number): string => {
    return formatFixed(value, 4);
  };

  const filteredData = data.filter(
    (item) =>
      selectedExchanges.length === 0 ||
      selectedExchanges.includes(item.exchange),
  );

  // Сортировка данных
  const sortedData = useMemo(() => {
    if (!filteredData.length || !sortConfig.direction) return filteredData;

    const { column, direction } = sortConfig;
    const multiplier = direction === "asc" ? 1 : -1;

    return [...filteredData].sort((a, b) => {
      let valueA: number | string | null;
      let valueB: number | string | null;

      if (column === "fundingRate") {
        valueA = a.fundingRate;
        valueB = b.fundingRate;
      } else if (column === "apr") {
        valueA = a.apr;
        valueB = b.apr;
      } else if (column === "markPrice") {
        valueA = a.markPrice;
        valueB = b.markPrice;
      } else {
        valueA = a.nextFundingTime
          ? new Date(a.nextFundingTime).getTime()
          : null;
        valueB = b.nextFundingTime
          ? new Date(b.nextFundingTime).getTime()
          : null;

        // null значения всегда в конце
        if (valueA === null && valueB === null) return 0;
        if (valueA === null) return 1;
        if (valueB === null) return -1;
      }

      if (typeof valueA === "number" && typeof valueB === "number") {
        return (valueA - valueB) * multiplier;
      }

      return 0;
    });
  }, [filteredData, sortConfig]);

  // Обработчик клика по заголовку
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

  // Иконка сортировки
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

  if (sortedData.length === 0) {
    return (
      <div
        className="flex items-center justify-center h-full"
        style={{ color: "var(--tg-text-tertiary)" }}
      >
        <p>Нет данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <div className="overflow-x-auto">
        <table className="w-full text-sm border-collapse">
          <thead
            className="sticky top-0 z-10"
            style={{ backgroundColor: "var(--tg-bg-secondary)" }}
          >
            <tr>
              <th
                className="px-4 py-1 text-left font-medium sticky left-0 z-20 min-w-[120px] border-b"
                style={{
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Биржа
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[140px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("markPrice")}
                style={{
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Цена маркировки</span>
                  <SortIcon column="markPrice" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[120px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("fundingRate")}
                style={{
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Ставка финансирования</span>
                  <SortIcon column="fundingRate" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[110px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("nextFundingTime")}
                style={{
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Время выплаты</span>
                  <SortIcon column="nextFundingTime" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center font-medium min-w-[110px] border-b cursor-pointer transition-colors"
                onClick={() => handleSort("apr")}
                style={{
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>APR</span>
                  <SortIcon column="apr" />
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            {sortedData.map((item) => (
              <tr
                key={`${item.exchange}-${item.symbol}`}
                className="border-t transition-colors"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <td
                  className="px-4 py-4 font-medium sticky left-0 z-10 border-r"
                  style={{
                    backgroundColor: "var(--tg-bg)",
                    color: "var(--tg-text)",
                    borderColor: "var(--tg-border)",
                  }}
                >
                  <div className="flex flex-col gap-1">
                    <span
                      className="px-3 py-1.5 rounded-xl text-sm font-semibold w-fit"
                      style={{
                        backgroundColor:
                          item.exchange === "Binance"
                            ? "rgba(241, 196, 15, 0.15)"
                            : item.exchange === "Bybit"
                              ? "rgba(230, 126, 34, 0.15)"
                              : "var(--tg-bg-tertiary)",
                        color:
                          item.exchange === "Binance"
                            ? "#F1C40F"
                            : item.exchange === "Bybit"
                              ? "#E67E22"
                              : "var(--tg-text-secondary)",
                      }}
                    >
                      {item.exchange}
                    </span>
                  </div>
                </td>
                <td
                  className="px-4 py-4 text-center border-l"
                  style={{ borderColor: "var(--tg-border)" }}
                >
                  <p
                    className="font-semibold text-base"
                    style={{ color: "var(--tg-text)" }}
                  >
                    $
                    {item.markPrice.toLocaleString(undefined, {
                      minimumFractionDigits: 4,
                      maximumFractionDigits: 4,
                    })}
                  </p>
                </td>
                <td
                  className="px-4 py-4 text-center border-l"
                  style={{ borderColor: "var(--tg-border)" }}
                >
                  <div className="flex flex-col items-center gap-1">
                    <p
                      className="text-lg font-bold"
                      style={{
                        color:
                          item.fundingRate > 0
                            ? "var(--tg-positive)"
                            : item.fundingRate < 0
                              ? "var(--tg-negative)"
                              : "var(--tg-text-tertiary)",
                      }}
                    >
                      {formatFundingPct(item.fundingRate * 100)}%
                    </p>
                    <p
                      className="text-xs"
                      style={{ color: "var(--tg-text-tertiary)" }}
                    >
                      {item.numberOfPaymentsPerDay} выплат/день
                    </p>
                  </div>
                </td>
                <td
                  className="px-4 py-4 text-center border-l"
                  style={{ borderColor: "var(--tg-border)" }}
                >
                  <p
                    className="font-semibold text-base"
                    style={{ color: "var(--tg-text)" }}
                  >
                    {item.nextFundingTime
                      ? new Date(item.nextFundingTime).toLocaleTimeString(
                          "ru-RU",
                          {
                            hour: "2-digit",
                            minute: "2-digit",
                          },
                        )
                      : "—"}
                  </p>
                </td>
                <td
                  className="px-4 py-4 text-center border-l"
                  style={{ borderColor: "var(--tg-border)" }}
                >
                  <p
                    className="text-lg font-bold"
                    style={{
                      color:
                        item.apr > 0
                          ? "var(--tg-positive)"
                          : item.apr < 0
                            ? "var(--tg-negative)"
                            : "var(--tg-text-tertiary)",
                    }}
                  >
                    {item.apr.toFixed(2)}%
                  </p>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
