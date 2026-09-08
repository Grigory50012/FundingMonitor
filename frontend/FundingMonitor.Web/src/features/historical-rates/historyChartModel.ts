import type { ExchangeType, HistoricalFundingRateDto } from "../../types";
import { TIME_RANGES } from "../../types/history";
import type { TimeRangeType } from "../../types/history";

export function filterHistoryByExchange(
  data: HistoricalFundingRateDto[],
  selectedExchanges: ExchangeType[],
) {
  if (selectedExchanges.length === 0) return data;
  return data.filter((item) => selectedExchanges.includes(item.exchange));
}

export function filterHistoryByTimeRange(
  data: HistoricalFundingRateDto[],
  timeRange: TimeRangeType,
) {
  const selectedRange = TIME_RANGES.find((range) => range.value === timeRange);
  if (!selectedRange) return data;

  const cutoffDate = new Date(
    Date.now() - selectedRange.days * 24 * 60 * 60 * 1000,
  );
  return data.filter((item) => new Date(item.fundingTime) >= cutoffDate);
}

export type HistoryChartPoint = {
  time: string;
  xAxisLabel: string;
  tooltipTime: string;
  timestamp: number;
  rawTime: Date;
  [key: string]: number | string | Date;
};

export function buildHistoryChartData(
  data: HistoricalFundingRateDto[],
): HistoryChartPoint[] {
  const timeMap = new Map<number, HistoryChartPoint>();
  const intervalData = new Map<
    number,
    {
      values: Map<string, { rate: number; time: Date }>;
      firstTime: Date;
    }
  >();

  data.forEach((item) => {
    const date = new Date(item.fundingTime);
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

    const interval = intervalData.get(intervalTimestamp);
    if (!interval) return;

    const existing = interval.values.get(item.exchange);
    if (!existing || date > existing.time) {
      interval.values.set(item.exchange, {
        rate: item.fundingRate * 100,
        time: date,
      });
    }

    if (date < interval.firstTime) {
      interval.firstTime = date;
    }
  });

  const dayToFirstTimestamp = new Map<string, number>();
  intervalData.forEach((_, timestamp) => {
    const date = new Date(timestamp);
    const dayKey = `${date.getUTCFullYear()}-${date.getUTCMonth()}-${date.getUTCDate()}`;

    if (!dayToFirstTimestamp.has(dayKey)) {
      dayToFirstTimestamp.set(dayKey, timestamp);
      return;
    }

    dayToFirstTimestamp.set(
      dayKey,
      Math.min(dayToFirstTimestamp.get(dayKey) ?? timestamp, timestamp),
    );
  });

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

    const dataPoint = timeMap.get(timestamp);
    if (!dataPoint) return;

    interval.values.forEach((intervalValue, exchange) => {
      dataPoint[exchange] = intervalValue.rate;
    });
  });

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
}
