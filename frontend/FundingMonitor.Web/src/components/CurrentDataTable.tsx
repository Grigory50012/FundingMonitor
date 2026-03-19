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

  // Функция форматирования числа с удалением лишних нулей
  const formatRate = (value: number): string => {
    // Форматируем с 6 знаками и убираем trailing zeros
    return value.toLocaleString(undefined, {
      minimumFractionDigits: 0,
      maximumFractionDigits: 6,
    });
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
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Нет данных для отображения</p>
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
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("markPrice")}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Mark Price</span>
                  <SortIcon column="markPrice" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("fundingRate")}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Funding Rate</span>
                  <SortIcon column="fundingRate" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("nextFundingTime")}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Next Funding</span>
                  <SortIcon column="nextFundingTime" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center text-gray-400 font-medium min-w-[140px] border-b border-gray-700 cursor-pointer hover:bg-gray-700 transition-colors"
                onClick={() => handleSort("apr")}
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
                className="border-t border-gray-800 hover:bg-gray-800/50 transition-colors"
              >
                <td className="px-4 py-4 text-white font-medium sticky left-0 bg-gray-900 z-10 border-r border-gray-700/50">
                  <div className="flex flex-col gap-1">
                    <span
                      className={`px-3 py-1.5 rounded-lg text-sm font-semibold w-fit ${
                        item.exchange === "Binance"
                          ? "bg-yellow-900/50 text-yellow-400"
                          : item.exchange === "Bybit"
                            ? "bg-orange-900/50 text-orange-400"
                            : "bg-gray-700 text-gray-400"
                      }`}
                    >
                      {item.exchange}
                    </span>
                    <span className="text-xs text-gray-500">{item.symbol}</span>
                  </div>
                </td>
                <td className="px-4 py-4 text-center border-l border-gray-800/50">
                  <p className="text-white font-semibold text-base">
                    $
                    {item.markPrice.toLocaleString(undefined, {
                      minimumFractionDigits: 4,
                      maximumFractionDigits: 8,
                    })}
                  </p>
                </td>
                <td className="px-4 py-4 text-center border-l border-gray-800/50">
                  <div className="flex flex-col items-center gap-1">
                    <p
                      className={`text-lg font-bold ${
                        item.fundingRate > 0
                          ? "text-green-400"
                          : item.fundingRate < 0
                            ? "text-red-400"
                            : "text-gray-400"
                      }`}
                    >
                      {formatRate(item.fundingRate * 100)}%
                    </p>
                    <p className="text-xs text-gray-600">
                      {item.numberOfPaymentsPerDay} выплат/день
                    </p>
                  </div>
                </td>
                <td className="px-4 py-4 text-center border-l border-gray-800/50">
                  <p className="text-white font-semibold text-base">
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
                <td className="px-4 py-4 text-center border-l border-gray-800/50">
                  <p
                    className={`text-lg font-bold ${
                      item.apr > 0
                        ? "text-green-400"
                        : item.apr < 0
                          ? "text-red-400"
                          : "text-gray-400"
                    }`}
                  >
                    {item.apr.toFixed(2)}%
                  </p>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Пояснение */}
      <div className="mt-4 pt-4 border-t border-gray-700">
        <p className="text-xs text-gray-500">
          <span className="text-green-400 font-medium">Funding Rate</span> —
          текущая ставка финансирования.
          <span className="mx-2">|</span>
          <span className="text-green-400 font-medium">APR</span> — годовой
          процент.
          <span className="mx-2">|</span>
          <span className="text-white font-medium">Mark Price</span> — расчётная
          цена.
          <span className="mx-2">|</span>
          <span className="text-gray-600">выплат/день</span> — количество выплат
          в сутки.
        </p>
      </div>
    </div>
  );
};
