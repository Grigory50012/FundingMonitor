import { useId, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { formatAssetPrice, toFullPrice } from "../lib/priceFormat";
import type { PriceFormat } from "../lib/priceFormat";

type PriceValueProps = {
  value: number | null | undefined;
  format: PriceFormat;
  showPlus?: boolean;
  className?: string;
};

export function PriceValue({ value, format, showPlus = false, className }: PriceValueProps) {
  const tooltipId = useId();
  const buttonRef = useRef<HTMLButtonElement>(null);
  const [tooltip, setTooltip] = useState<{ left: number; top: number; below: boolean } | null>(null);
  const [copyResult, setCopyResult] = useState<{ value: number; message: string } | null>(null);
  const valid = value != null && Number.isFinite(value);
  const fullValue = valid ? toFullPrice(value) : "";
  const message = copyResult && copyResult.value === value ? copyResult.message : "";

  const showTooltip = () => {
    const rect = buttonRef.current?.getBoundingClientRect();
    if (!rect) return;
    const below = rect.top < 100;
    setTooltip({
      left: Math.max(8, Math.min(rect.left, window.innerWidth - 288)),
      top: below ? rect.bottom + 8 : rect.top - 8,
      below,
    });
  };

  if (!valid) return <span className={className}>—</span>;

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        className={`cursor-copy rounded-sm text-inherit focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--tg-button)] ${className ?? ""}`}
        aria-label={`Скопировать ${showPlus ? "спред" : "цену"} ${fullValue}`}
        aria-describedby={tooltip ? tooltipId : undefined}
        onMouseEnter={showTooltip}
        onMouseLeave={() => setTooltip(null)}
        onFocus={showTooltip}
        onBlur={() => { setTooltip(null); setCopyResult(null); }}
        onKeyDown={(event) => {
          if (event.key === "Escape") setTooltip(null);
          if (["Enter", " ", "Escape"].includes(event.key)) event.stopPropagation();
        }}
        onClick={async (event) => {
          event.stopPropagation();
          setCopyResult(null);
          showTooltip();
          try {
            await navigator.clipboard.writeText(fullValue);
            setCopyResult({ value, message: "Скопировано" });
          } catch {
            setCopyResult({ value, message: "Не удалось скопировать" });
          }
        }}
      >
        {formatAssetPrice(value, format, undefined, showPlus)}
      </button>
      <span className="sr-only" role="status">{message}</span>
      {tooltip && createPortal(
        <span
          id={tooltipId}
          role="tooltip"
          className="pointer-events-none fixed z-[1000] w-max max-w-[min(280px,calc(100vw-16px))] rounded border border-[var(--tg-border)] bg-[var(--tg-bg-secondary)] px-2 py-1 text-xs text-[var(--tg-text)] shadow-lg [overflow-wrap:anywhere]"
          style={{ left: tooltip.left, top: tooltip.top, transform: tooltip.below ? undefined : "translateY(-100%)" }}
        >
          {fullValue}
          {message ? <span className="block">{message}</span> : null}
        </span>,
        document.body,
      )}
    </>
  );
}
