type FractionOptions = {
  minimumFractionDigits?: number;
  maximumFractionDigits?: number;
};

export function formatNumber(
  value: number,
  options: FractionOptions = {},
): string {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: options.minimumFractionDigits ?? 0,
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
  });
}

export function formatPercent(
  value: number,
  options: FractionOptions = {},
): string {
  return `${formatNumber(value, {
    minimumFractionDigits: options.minimumFractionDigits ?? 2,
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
  })}%`;
}

export function formatPrice(
  value: number,
  options: FractionOptions = {},
): string {
  return `$${formatNumber(value, {
    minimumFractionDigits: options.minimumFractionDigits ?? 2,
    maximumFractionDigits: options.maximumFractionDigits ?? 8,
  })}`;
}

export function formatTime(value: string | null): string {
  if (!value) return "-";

  return new Date(value).toLocaleTimeString("ru-RU", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function getSignedColor(value: number): string {
  if (value > 0) return "var(--tg-positive)";
  if (value < 0) return "var(--tg-negative)";
  return "var(--tg-text-tertiary)";
}
