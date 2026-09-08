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
import { EXCHANGE_COLORS } from "../../entities/exchange/model";
import type { HistoricalFundingRateDto, ExchangeType } from "../../types";
import type { TimeRangeType } from "../../types/history";
import { TIME_RANGES } from "../../types/history";
import {
  buildHistoryChartData,
  filterHistoryByExchange,
  filterHistoryByTimeRange,
} from "./historyChartModel";

interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
  timeRange?: TimeRangeType;
  onTimeRangeChange?: (range: TimeRangeType) => void;
}

export const HistoryChartPanel: React.FC<HistoryPanelProps> = ({
  data,
  selectedExchanges,
  timeRange = "1m",
  onTimeRangeChange,
}) => {
  const filteredData = React.useMemo(
    () => filterHistoryByExchange(data, selectedExchanges),
    [data, selectedExchanges],
  );

  const timeFilteredData = React.useMemo(
    () => filterHistoryByTimeRange(filteredData, timeRange),
    [filteredData, timeRange],
  );

  const chartData = React.useMemo(
    () => buildHistoryChartData(timeFilteredData),
    [timeFilteredData],
  );

  // Получаем уникальные биржи из всех данных (чтобы линии не пропадали при фильтрации)
  const allExchanges = React.useMemo(
    () => Array.from(new Set(filteredData.map((item) => item.exchange))),
    [filteredData],
  );

  type TooltipDatum = {
    time?: string;
    xAxisLabel?: string;
    tooltipTime?: string;
    timestamp?: number;
    rawTime?: Date;
    [key: string]: number | string | Date | undefined;
  };
  type TooltipPayloadEntry = {
    dataKey?: string;
    color?: string;
    payload?: TooltipDatum;
  };
  type TooltipPayload = TooltipPayloadEntry[];
  const CustomTooltip: React.FC<{ active?: boolean; payload?: TooltipPayload }>
    = ({ active, payload }) => {
    if (active && payload && payload.length) {
      const payloadData = payload[0]?.payload;
      const rawTime = payloadData?.rawTime as Date | undefined;
      const dateStr = rawTime
        ? new Date(rawTime).toLocaleString("ru-RU", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })
        : (payloadData?.tooltipTime ?? "00:00");

      return (
        <div className="rounded-md p-2 shadow-xl" style={{ backgroundColor: 'var(--tg-bg-secondary)', border: '1px solid var(--tg-border)' }}>
          <p className="text-[11px] mb-1 font-medium" style={{ color: 'var(--tg-text-secondary)' }}>{dateStr}</p>
          {/* Показываем все биржи, а не только те, что есть в payload */}
          {allExchanges.map((exchange) => {
            const value = payloadData ? (payloadData as { [key: string]: number | string | Date | undefined })[exchange] : undefined;
            if (typeof value !== "number") return null;

            // Находим цвет для биржи
            const line = payload.find((p) => p.dataKey === exchange);
            const color = line?.color || EXCHANGE_COLORS[exchange as ExchangeType];

            return (
              <div key={exchange} className="flex items-center gap-1.5 text-[11px]">
                <span className="w-2 h-2 rounded-full flex-shrink-0" style={{ backgroundColor: color }} />
                <span style={{ color: 'var(--tg-text-secondary)' }}>{exchange}:</span>
                <span
                  className="font-bold tabular-nums ml-auto"
                  style={{
                    color: value > 0 ? 'var(--tg-positive)' : value < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                  }}
                >
                  {value.toFixed(4)}%
                </span>
              </div>
            );
          })}
        </div>
      );
    }
    return null;
  };

  if (timeFilteredData.length === 0) {
    return (
      <div className="flex items-center justify-center h-full" style={{ color: 'var(--tg-text-tertiary)' }}>
        <p className="text-xs">Нет исторических данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Переключатель временных диапазонов */}
      {onTimeRangeChange && (
        <div className="flex items-center gap-1 mb-2">
          {TIME_RANGES.map((range) => (
            <button
              key={range.value}
              onClick={() => onTimeRangeChange(range.value)}
              className="px-2 py-1 rounded-md text-xs font-medium transition-all"
              style={{
                backgroundColor: timeRange === range.value ? 'var(--tg-button)' : 'var(--tg-bg-tertiary)',
                color: timeRange === range.value ? 'var(--tg-button-text)' : 'var(--tg-text-secondary)',
              }}
            >
              {range.label}
            </button>
          ))}
        </div>
      )}

      <div className="flex-1 min-h-0">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart
            key={timeRange}
            data={chartData}
            margin={{ top: 10, right: 20, bottom: 30, left: 0 }}
          >
            <CartesianGrid
              strokeDasharray="4 4"
              stroke="#4B5563"
              vertical={true}
              horizontal={true}
            />
            <XAxis
              dataKey="time"
              stroke="#9CA3AF"
              tick={{ fontSize: 11 }}
              tickFormatter={(_value, index) => {
                const point = chartData[index];
                return point?.xAxisLabel || "";
              }}
              angle={-45}
              textAnchor="end"
              height={80}
              interval={0}
              tickMargin={10}
            />
            <YAxis
              stroke="#9CA3AF"
              tick={{ fontSize: 11 }}
              tickFormatter={(value) => `${value.toFixed(2)}%`}
              width={50}
            />
            <Tooltip content={<CustomTooltip />} />
            <Legend
              wrapperStyle={{ fontSize: "11px", paddingTop: "10px" }}
              formatter={(value) => (
                <span style={{ color: 'var(--tg-text)' }}>{value}</span>
              )}
            />
            <ReferenceLine y={0} stroke="#9CA3AF" strokeDasharray="4 4" />
            {allExchanges.map((exchange) => (
              <Line
                key={exchange}
                type="monotone"
                dataKey={exchange}
                stroke={EXCHANGE_COLORS[exchange as ExchangeType]}
                strokeWidth={2.5}
                dot={false}
                activeDot={{ r: 7, strokeWidth: 2 }}
                connectNulls={true}
                animationDuration={500}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Таблица с последними значениями */}
      <div className="mt-2 pt-2 border-t" style={{ borderColor: 'var(--tg-border)' }}>
        <h3 className="text-xs font-medium mb-1.5" style={{ color: 'var(--tg-text-secondary)' }}>
          Последние значения
        </h3>
        <div className="grid grid-cols-3 gap-1.5">
          {allExchanges.map((exchange) => {
            const lastValue = timeFilteredData
              .filter((item) => item.exchange === exchange)
              .sort(
                (a, b) =>
                  new Date(b.fundingTime).getTime() -
                  new Date(a.fundingTime).getTime(),
              )[0];

            // Если для этой биржи нет данных в выбранном диапазоне, пропускаем
            if (!lastValue) return null;

            return (
              <div
                key={exchange}
                className="rounded-md p-1.5 text-center"
                style={{ backgroundColor: 'var(--tg-bg-tertiary)' }}
              >
                <p className="text-[10px] leading-none mb-0.5" style={{ color: 'var(--tg-text-tertiary)' }}>{exchange}</p>
                <p
                  className="text-xs font-bold tabular-nums leading-tight"
                  style={{
                    color: lastValue.fundingRate > 0 ? 'var(--tg-positive)' : lastValue.fundingRate < 0 ? 'var(--tg-negative)' : 'var(--tg-text-tertiary)',
                  }}
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
