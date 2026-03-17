import React from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ReferenceLine,
} from "recharts";
import type { HistoricalFundingRateDto, ExchangeType } from "../types";

interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
}

export const HistoryPanel: React.FC<HistoryPanelProps> = ({
  data,
  selectedExchanges,
}) => {
  // Фильтруем данные по выбранным биржам
  const filteredData = data.filter(
    (item) =>
      selectedExchanges.length === 0 ||
      selectedExchanges.includes(item.exchange),
  );

  // Группируем данные по времени для графика
  const chartData = React.useMemo(() => {
    const timeMap = new Map<
      string,
      { time: string; timestamp: number; [key: string]: number | string }
    >();

    filteredData.forEach((item) => {
      const timestamp = new Date(item.fundingTime).getTime();
      const timeKey = item.fundingTime;

      if (!timeMap.has(timeKey)) {
        timeMap.set(timeKey, {
          time: new Date(item.fundingTime).toLocaleString("ru-RU", {
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
          }),
          timestamp,
        });
      }

      const dataPoint = timeMap.get(timeKey)!;
      dataPoint[`${item.exchange}`] = item.fundingRate * 100;
    });

    return Array.from(timeMap.values()).sort(
      (a, b) => a.timestamp - b.timestamp,
    );
  }, [filteredData]);

  // Получаем уникальные биржи из данных
  const uniqueExchanges = Array.from(
    new Set(filteredData.map((item) => item.exchange)),
  );

  // Цвета для бирж
  const exchangeColors: Record<ExchangeType, string> = {
    Binance: "#F0B90B",
    Bybit: "#FFA500",
    OKX: "#000000",
  };

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="bg-gray-800 border border-gray-700 rounded-lg p-3 shadow-xl">
          <p className="text-gray-400 text-sm mb-2">{label}</p>
          {payload.map((entry: any, index: number) => (
            <div key={index} className="flex items-center gap-2 text-sm">
              <span
                className="w-3 h-3 rounded-full"
                style={{ backgroundColor: entry.color }}
              />
              <span className="text-gray-300">{entry.name}:</span>
              <span
                className={`font-semibold ${
                  entry.value > 0
                    ? "text-green-400"
                    : entry.value < 0
                      ? "text-red-400"
                      : "text-gray-400"
                }`}
              >
                {entry.value.toFixed(4)}%
              </span>
            </div>
          ))}
        </div>
      );
    }
    return null;
  };

  if (filteredData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        <p>Нет исторических данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      <h2 className="text-lg font-semibold text-white mb-4">История ставок</h2>
      <div className="flex-1 min-h-0">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart
            data={chartData}
            margin={{ top: 5, right: 20, bottom: 25, left: 0 }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
            <XAxis
              dataKey="time"
              stroke="#9CA3AF"
              tick={{ fontSize: 11 }}
              tickFormatter={(value) => value.split(" ")[0]}
              angle={-45}
              textAnchor="end"
              height={60}
            />
            <YAxis
              stroke="#9CA3AF"
              tick={{ fontSize: 12 }}
              tickFormatter={(value) => `${value.toFixed(2)}%`}
            />
            <Tooltip content={<CustomTooltip />} />
            <Legend
              wrapperStyle={{ fontSize: "12px", paddingTop: "10px" }}
              formatter={(value) => (
                <span style={{ color: "#E5E7EB" }}>{value}</span>
              )}
            />
            <ReferenceLine y={0} stroke="#6B7280" strokeDasharray="3 3" />
            {uniqueExchanges.map((exchange) => (
              <Line
                key={exchange}
                type="monotone"
                dataKey={exchange}
                stroke={exchangeColors[exchange as ExchangeType]}
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 6, strokeWidth: 0 }}
                connectNulls={false}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Таблица с последними значениями */}
      <div className="mt-4 pt-4 border-t border-gray-700">
        <h3 className="text-sm font-medium text-gray-400 mb-2">
          Последние значения
        </h3>
        <div className="grid grid-cols-3 gap-2">
          {uniqueExchanges.map((exchange) => {
            const lastValue = filteredData
              .filter((item) => item.exchange === exchange)
              .sort(
                (a, b) =>
                  new Date(b.fundingTime).getTime() -
                  new Date(a.fundingTime).getTime(),
              )[0];

            return (
              <div
                key={exchange}
                className="bg-gray-800 rounded-lg p-2 text-center"
              >
                <p className="text-xs text-gray-500">{exchange}</p>
                <p
                  className={`text-sm font-semibold ${
                    lastValue.fundingRate > 0
                      ? "text-green-400"
                      : lastValue.fundingRate < 0
                        ? "text-red-400"
                        : "text-gray-400"
                  }`}
                >
                  {(lastValue.fundingRate * 100).toFixed(4)}%
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
