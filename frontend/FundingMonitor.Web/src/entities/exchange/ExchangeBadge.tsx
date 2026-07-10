import { getExchangeTone } from "./model";

type ExchangeBadgeProps = {
  exchange: string;
  compact?: boolean;
};

export function ExchangeBadge({
  exchange,
  compact = false,
}: ExchangeBadgeProps) {
  const tone = getExchangeTone(exchange);

  return (
    <span
      className={`${
        compact ? "px-1.5 py-0.5 text-[10px]" : "px-2 py-1 text-xs"
      } rounded-md font-semibold inline-block text-center`}
      style={tone}
    >
      {exchange}
    </span>
  );
}
