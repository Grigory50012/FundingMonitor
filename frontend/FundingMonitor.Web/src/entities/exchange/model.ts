import type { ExchangeType } from "../../types";

export const EXCHANGES: ExchangeType[] = ["Binance", "Bybit", "OKX"];

export const EXCHANGE_COLORS: Record<ExchangeType, string> = {
  Binance: "#f0760b",
  Bybit: "#ffcc00",
  OKX: "#FFFFFF",
};

export type ExchangeTone = {
  backgroundColor: string;
  color: string;
};

export function getExchangeTone(exchange: string): ExchangeTone {
  if (exchange === "Binance") {
    return { backgroundColor: "rgba(241, 196, 15, 0.15)", color: "#F1C40F" };
  }

  if (exchange === "Bybit") {
    return { backgroundColor: "rgba(230, 126, 34, 0.15)", color: "#E67E22" };
  }

  return {
    backgroundColor: "var(--tg-bg-tertiary)",
    color: "var(--tg-text-secondary)",
  };
}
