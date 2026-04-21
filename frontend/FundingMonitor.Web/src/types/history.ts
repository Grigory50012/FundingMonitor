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
