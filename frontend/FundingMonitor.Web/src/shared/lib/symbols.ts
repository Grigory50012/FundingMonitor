export function toBaseSymbol(symbol: string): string {
  return symbol.replace("-USDT", "");
}

export function toUsdtSymbol(symbol: string): string {
  const trimmed = symbol.trim();
  if (!trimmed) return "";
  return trimmed.includes("-") ? trimmed : `${trimmed}-USDT`;
}
