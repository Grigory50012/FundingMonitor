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
import { EXCHANGE_COLORS } from "../types";

export type TimeRangeType = "1d" | "2d" | "3d" | "1w" | "2w" | "3w" | "1m";

export const TIME_RANGES: {
  value: TimeRangeType;
  label: string;
  days: number;
}[] = [
  { value: "1d", label: "1 день", days: 1 },
  { value: "2d", label: "2 дня", days: 2 },
  { value: "3d", label: "3 дня", days: 3 },
  { value: "1w", label: "1 неделя", days: 7 },
  { value: "2w", label: "2 недели", days: 14 },
  { value: "3w", label: "3 недели", days: 21 },
  { value: "1m", label: "1 месяц", days: 30 },
];

interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];
  selectedExchanges: ExchangeType[];
  timeRange?: TimeRangeType;
  onTimeRangeChange?: (range: TimeRangeType) => void;
}

export const HistoryPanel: React.FC<HistoryPanelProps> = ({
  data,
  selectedExchanges,
  timeRange = "1m",
  onTimeRangeChange,
}) => {
  // Фильтруем данные по выбранным биржам
  const filteredData = data.filter(
    (item) =>
      selectedExchanges.length === 0 ||
      selectedExchanges.includes(item.exchange),
  );

  // Фильтруем данные по выбранному временному диапазону
  const timeFilteredData = React.useMemo(() => {
    const selectedRange = TIME_RANGES.find((r) => r.value === timeRange);
    if (!selectedRange) return filteredData;

    const now = new Date();
    const cutoffDate = new Date(
      now.getTime() - selectedRange.days * 24 * 60 * 60 * 1000,
    );

    return filteredData.filter(
      (item) => new Date(item.fundingTime) >= cutoffDate,
    );
  }, [filteredData, timeRange]);

  // Группируем данные по временным меткам для отображения на графике
  const chartData = React.useMemo(() => {
    const timeMap = new Map<
      number,
      {
        time: string;
        xAxisLabel: string;
        tooltipTime: string;
        timestamp: number;
        rawTime: Date;
        [key: string]: number | string | Date;
      }
    >();

    // Собираем данные по часовым интервалам (чтобы объединить данные бирж)
    const intervalData = new Map<
      number,
      {
        values: Map<string, { rate: number; time: Date }>;
        firstTime: Date;
      }
    >();

    timeFilteredData.forEach((item) => {
      const date = new Date(item.fundingTime);
      // Округляем до часа, чтобы объединить данные Binance и Bybit
      const intervalDate = new Date(
        Date.UTC(
          date.getUTCFullYear(),
          date.getUTCMonth(),
          date.getUTCDate(),
          date.getUTCHours(),
          0,
          0,
          0,
        ),
      );
      const intervalTimestamp = intervalDate.getTime();

      if (!intervalData.has(intervalTimestamp)) {
        intervalData.set(intervalTimestamp, {
          values: new Map(),
          firstTime: date,
        });
      }

      const interval = intervalData.get(intervalTimestamp)!;
      // Берём последнее значение для этой биржи в интервале
      const existing = interval.values.get(item.exchange);
      if (!existing || date > existing.time) {
        interval.values.set(item.exchange, {
          rate: item.fundingRate * 100,
          time: date,
        });
      }
      // Обновляем первое время для tooltip
      if (date < interval.firstTime) {
        interval.firstTime = date;
      }
    });

    // Собираем все интервалы по дням для определения первой точки дня
    const dayToFirstTimestamp = new Map<string, number>();
    intervalData.forEach((_, timestamp) => {
      const date = new Date(timestamp);
      const dayKey = `${date.getUTCFullYear()}-${date.getUTCMonth()}-${date.getUTCDate()}`;

      if (!dayToFirstTimestamp.has(dayKey)) {
        dayToFirstTimestamp.set(dayKey, timestamp);
      } else {
        dayToFirstTimestamp.set(
          dayKey,
          Math.min(dayToFirstTimestamp.get(dayKey)!, timestamp),
        );
      }
    });

    // Преобразуем в формат для графика
    intervalData.forEach((interval, timestamp) => {
      const date = new Date(timestamp);
      timeMap.set(timestamp, {
        time: date.toLocaleString("ru-RU", {
          month: "short",
          day: "numeric",
          hour: "2-digit",
        }),
        xAxisLabel: "",
        tooltipTime: interval.firstTime.toLocaleString("ru-RU", {
          hour: "2-digit",
          minute: "2-digit",
        }),
        timestamp,
        rawTime: interval.firstTime,
      });

      const dataPoint = timeMap.get(timestamp)!;
      interval.values.forEach((data, exchange) => {
        dataPoint[exchange] = data.rate;
      });
    });

    // Помечаем первую точку каждого дня
    const result = Array.from(timeMap.values()).sort(
      (a, b) => a.timestamp - b.timestamp,
    );

    result.forEach((point) => {
      const pointDate = new Date(point.timestamp);
      const dayKey = `${pointDate.getUTCFullYear()}-${pointDate.getUTCMonth()}-${pointDate.getUTCDate()}`;
      const firstTimestampOfDay = dayToFirstTimestamp.get(dayKey);
      point.xAxisLabel =
        point.timestamp === firstTimestampOfDay
          ? pointDate.toLocaleString("ru-RU", {
              month: "short",
              day: "numeric",
            })
          : "";
    });

    return result;
  }, [timeFilteredData]);

  // Получаем уникальные биржи из всех данных (чтобы линии не пропадали при фильтрации)
  const allExchanges = React.useMemo(
    () => Array.from(new Set(filteredData.map((item) => item.exchange))),
    [filteredData],
  );

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const payloadData = payload[0]?.payload;
      const rawTime = payloadData?.rawTime;
      const dateStr = rawTime
        ? new Date(rawTime).toLocaleString("ru-RU", {
            day: "2-digit",
            month: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
          })
        : payloadData?.tooltipTime || "00:00";

      return (
        <div className="rounded-xl p-3 shadow-xl" style={{ backgroundColor: 'var(--tg-bg-secondary)', border: '1px solid var(--tg-border)' }}>
          <p className="text-sm mb-2 font-medium" style={{ color: 'var(--tg-text-secondary)' }}>{dateStr}</p>
          {/* Показываем все биржи, а не только те, что есть в payload */}
          {allExchanges.map((exchange) => {
            const value = payloadData?.[exchange];
            if (value === undefined || value === null) return null;

            // Находим цвет для биржи
            const line = payload.find((p: any) => p.dataKey === exchange);
            const color =
              line?.color || EXCHANGE_COLORS[exchange as ExchangeType];

            return (
              <div key={exchange} className="flex items-center gap-2 text-sm">
                <span
                  className="w-3 h-3 rounded-full"
                  style={{ backgroundColor: color }}
                />
                <span style={{ color: 'var(--tg-text-secondary)' }}>{exchange}:</span>
                <span
                  className="font-semibold"
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
        <p>Нет исторических данных для отображения</p>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Переключатель временных диапазонов */}
      {onTimeRangeChange && (
        <div className="flex items-center gap-2 mb-4">
          {TIME_RANGES.map((range) => (
            <button
              key={range.value}
              onClick={() => onTimeRangeChange(range.value)}
              className="px-3 py-1.5 rounded-xl text-sm font-medium transition-all"
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
              wrapperStyle={{ fontSize: "12px", paddingTop: "15px" }}
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
      <div className="mt-4 pt-4 border-t" style={{ borderColor: 'var(--tg-border)' }}>
        <h3 className="text-sm font-medium mb-2" style={{ color: 'var(--tg-text-secondary)' }}>
          Последние значения
        </h3>
        <div className="grid grid-cols-3 gap-2">
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
                className="rounded-xl p-2 text-center"
                style={{ backgroundColor: 'var(--tg-bg-tertiary)' }}
              >
                <p className="text-xs" style={{ color: 'var(--tg-text-tertiary)' }}>{exchange}</p>
                <p
                  className="text-sm font-semibold"
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
