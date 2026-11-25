export type PriceFormat = Readonly<{
  fractionDigits: number;
  compact: boolean;
}>;

export type PriceSample = Readonly<{
  symbol: string;
  value: number | null | undefined;
}>;

/** Expands the decimal representation available in the frontend, without rounding. */
export function toFullPrice(value: number): string {
  if (!Number.isFinite(value)) return "—";
  const sign = value < 0 ? "-" : "";
  const [coefficient, exponent = "0"] = Math.abs(value).toString().split("e");
  const [integer, fraction = ""] = coefficient.split(".");
  const digits = integer + fraction;
  const point = integer.length + Number(exponent);
  if (point <= 0) return `${sign}0.${"0".repeat(-point)}${digits}`;
  if (point >= digits.length) return sign + digits.padEnd(point, "0");
  return `${sign}${digits.slice(0, point)}.${digits.slice(point)}`;
}

function leadingZeros(fraction: string): number {
  return fraction.search(/[1-9]/);
}

/** Chooses a single format per symbol from the complete loaded snapshot. */
export function buildPriceFormats(samples: readonly PriceSample[]): Map<string, PriceFormat> {
  const formats = new Map<string, PriceFormat>();
  const validSymbols = new Set<string>();
  for (const { symbol, value } of samples) {
    if (!formats.has(symbol)) formats.set(symbol, { fractionDigits: 2, compact: false });
    if (value == null || !Number.isFinite(value) || value === 0) continue;

    const absolute = Math.abs(value);
    const fractionDigits = absolute >= 1000 ? 2 : absolute >= 1 ? 4
      : leadingZeros(toFullPrice(absolute).split(".")[1]) + 5;
    const compact = absolute < 0.001;
    const previous = validSymbols.has(symbol) ? formats.get(symbol) : undefined;
    formats.set(symbol, {
      fractionDigits: Math.max(previous?.fractionDigits ?? 0, fractionDigits),
      compact: compact && (previous?.compact ?? true),
    });
    validSymbols.add(symbol);
  }
  return formats;
}

type LocaleParts = {
  integer: Intl.NumberFormat;
  decimal: string;
  digits: string[];
};

// Reuse Intl instances across cells; bound the cache for callers selecting locales.
const localeCache = new Map<string | undefined, LocaleParts>();
function getLocaleParts(locale?: string): LocaleParts {
  const cached = localeCache.get(locale);
  if (cached) return cached;
  const digitFormatter = new Intl.NumberFormat(locale, { useGrouping: false });
  const parts = {
    integer: new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }),
    decimal: digitFormatter.formatToParts(1.1).find((part) => part.type === "decimal")?.value ?? ".",
    digits: Array.from({ length: 10 }, (_, digit) => digitFormatter.format(digit)),
  };
  if (localeCache.size >= 16) localeCache.clear();
  localeCache.set(locale, parts);
  return parts;
}

/** Truncates decimal text before localization; never feeds the fraction back into a number. */
export function formatAssetPrice(
  value: number | null | undefined,
  format: PriceFormat,
  locale?: string,
  showPlus = false,
): string {
  if (value == null || !Number.isFinite(value)) return "—";
  const [integer, sourceFraction = ""] = toFullPrice(Math.abs(value)).split(".");
  const fractionDigits = value === 0 && !showPlus ? 2 : format.fractionDigits;
  const fraction = sourceFraction.slice(0, fractionDigits).padEnd(fractionDigits, "0");
  const nonzero = /[1-9]/.test(integer + fraction);
  const sign = !nonzero ? "" : value < 0 ? "-" : showPlus ? "+" : "";
  const localized = getLocaleParts(locale);
  const localizeDigits = (text: string) => text.replace(/\d/g, (digit) => localized.digits[Number(digit)]);
  const zeroCount = leadingZeros(fraction);
  const compact = format.compact && integer === "0" && zeroCount > 0;
  const displayedFraction = compact
    ? `${localized.digits[0]}${zeroCount.toString().replace(/\d/g, (digit) => "₀₁₂₃₄₅₆₇₈₉"[Number(digit)])}${localizeDigits(fraction.slice(zeroCount))}`
    : localizeDigits(fraction);
  return `${sign}$${localized.integer.format(BigInt(integer))}${fraction ? localized.decimal + displayedFraction : ""}`;
}
