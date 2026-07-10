export type TimeRangeType = "1d" | "2d" | "3d" | "1w" | "2w" | "3w" | "1m";

export const TIME_RANGES: {
  value: TimeRangeType;
  label: string;
  days: number;
}[] = [
  { value: "1d", label: "1 \u0434\u0435\u043d\u044c", days: 1 },
  { value: "2d", label: "2 \u0434\u043d\u044f", days: 2 },
  { value: "3d", label: "3 \u0434\u043d\u044f", days: 3 },
  { value: "1w", label: "1 \u043d\u0435\u0434\u0435\u043b\u044f", days: 7 },
  { value: "2w", label: "2 \u043d\u0435\u0434\u0435\u043b\u0438", days: 14 },
  { value: "3w", label: "3 \u043d\u0435\u0434\u0435\u043b\u0438", days: 21 },
  { value: "1m", label: "1 \u043c\u0435\u0441\u044f\u0446", days: 30 },
];
