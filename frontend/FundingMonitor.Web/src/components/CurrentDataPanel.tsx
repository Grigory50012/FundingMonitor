import React from "react";
import type { FundingRateDto, ExchangeType } from "../types";

interface CurrentDataPanelProps {
  data: FundingRateDto[];
  selectedExchanges: ExchangeType[];
}

export const CurrentDataPanel: React.FC<CurrentDataPanelProps> = ({
  data,
  selectedExchanges,
}) => {
  const filteredData = data.filter(
    (item) =>
      selectedExchanges.length === 0 ||
      selectedExchanges.includes(item.exchange),
  );

  if (filteredData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Нет данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto">
      <h2 className="text-lg font-semibold text-white mb-4 sticky top-0 py-2">
        Текущие данные
      </h2>
      <div className="space-y-4">
        {filteredData.map((item) => (
          <div
            key={`${item.exchange}-${item.symbol}`}
            className="bg-gray-800 rounded-xl p-4 border border-gray-700 hover:border-gray-600 transition-colors"
          >
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg font-bold text-white">
                  {item.symbol}
                </span>
                <span
                  className={`px-2 py-0.5 rounded text-xs font-medium ${
                    item.fundingRate > 0
                      ? "bg-green-900/50 text-green-400"
                      : item.fundingRate < 0
                        ? "bg-red-900/50 text-red-400"
                        : "bg-gray-700 text-gray-400"
                  }`}
                >
                  {item.exchange}
                </span>
              </div>
              <span className="text-xs text-gray-500">
                Выплат в день: {item.numberOfPaymentsPerDay}
              </span>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="bg-gray-900/50 rounded-lg p-3">
                <p className="text-xs text-gray-500 mb-1">Funding Rate</p>
                <p
                  className={`text-lg font-semibold ${
                    item.fundingRate > 0
                      ? "text-green-400"
                      : item.fundingRate < 0
                        ? "text-red-400"
                        : "text-gray-400"
                  }`}
                >
                  {(item.fundingRate * 100).toFixed(4)}%
                </p>
              </div>

              <div className="bg-gray-900/50 rounded-lg p-3">
                <p className="text-xs text-gray-500 mb-1">APR</p>
                <p
                  className={`text-lg font-semibold ${
                    item.apr > 0
                      ? "text-green-400"
                      : item.apr < 0
                        ? "text-red-400"
                        : "text-gray-400"
                  }`}
                >
                  {item.apr.toFixed(2)}%
                </p>
              </div>

              <div className="bg-gray-900/50 rounded-lg p-3">
                <p className="text-xs text-gray-500 mb-1">Mark Price</p>
                <p className="text-lg font-semibold text-white">
                  $
                  {item.markPrice.toLocaleString(undefined, {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  })}
                </p>
              </div>

              <div className="bg-gray-900/50 rounded-lg p-3">
                <p className="text-xs text-gray-500 mb-1">Next Funding</p>
                <p className="text-lg font-semibold text-white">
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
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
