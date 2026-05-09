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
    return formatFixed(value, 3);
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
    <div className="h-full overflow-y-auto overflow-x-hidden min-w-0">
      <table className="w-full table-fixed text-xs border-separate border-spacing-0 [&_tbody_td]:border-b [&_tbody_td]:border-[var(--tg-border)]">
        <colgroup>
          <col className="w-[20%]" />
          <col className="w-[22%]" />
          <col className="w-[26%]" />
          <col className="w-[14%]" />
          <col className="w-[18%]" />
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
            <th
              className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
              onClick={() => handleSort("markPrice")}
              style={{
                position: "sticky",
                top: 0,
                backgroundColor: "var(--tg-bg-secondary)",
                color: "var(--tg-text-secondary)",
                borderColor: "var(--tg-border)",
              }}
            >
              <div className="flex items-center justify-center gap-0.5 flex-wrap">
                <span className="leading-tight">Марк.</span>
                <SortIcon column="markPrice" />
              </div>
            </th>
            <th
              className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
              onClick={() => handleSort("fundingRate")}
              style={{
                position: "sticky",
                top: 0,
                backgroundColor: "var(--tg-bg-secondary)",
                color: "var(--tg-text-secondary)",
                borderColor: "var(--tg-border)",
              }}
            >
              <div className="flex items-center justify-center gap-0.5 flex-wrap">
                <span className="leading-tight">Ставка</span>
                <SortIcon column="fundingRate" />
              </div>
            </th>
            <th
              className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
              onClick={() => handleSort("nextFundingTime")}
              style={{
                position: "sticky",
                top: 0,
                backgroundColor: "var(--tg-bg-secondary)",
                color: "var(--tg-text-secondary)",
                borderColor: "var(--tg-border)",
              }}
            >
              <div className="flex items-center justify-center gap-0.5 flex-wrap">
                <span className="leading-tight">Время</span>
                <SortIcon column="nextFundingTime" />
              </div>
            </th>
            <th
              className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
              onClick={() => handleSort("apr")}
              style={{
                position: "sticky",
                top: 0,
                backgroundColor: "var(--tg-bg-secondary)",
                color: "var(--tg-text-secondary)",
                borderColor: "var(--tg-border)",
              }}
            >
              <div className="flex items-center justify-center gap-0.5 flex-wrap">
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
                className="px-1.5 py-2 font-medium sticky left-0 z-10 border-r align-middle text-center"
                style={{
                  backgroundColor: "var(--tg-bg)",
                  color: "var(--tg-text)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex flex-col items-center justify-center gap-0.5 min-w-0">
                  <span
                    className="px-1.5 py-0.5 rounded-md text-[10px] font-semibold mx-auto max-w-full truncate inline-block text-center"
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
                className="px-1.5 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="font-semibold text-[11px] tabular-nums leading-tight break-all text-center"
                  style={{ color: "var(--tg-text)" }}
                  title={`$${item.markPrice}`}
                >
                  $
                  {item.markPrice.toLocaleString(undefined, {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  })}
                </p>
              </td>
              <td
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <div className="flex flex-col items-center justify-center gap-0.5 leading-tight">
                  <p
                    className="text-xs font-bold tabular-nums"
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
                    className="text-[10px] leading-none"
                    style={{ color: "var(--tg-text-tertiary)" }}
                  >
                    {item.numberOfPaymentsPerDay} вып./день
                  </p>
                </div>
              </td>
              <td
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="font-medium text-[11px] tabular-nums text-center"
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
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="text-xs font-bold tabular-nums text-center mx-auto"
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
  );
};
