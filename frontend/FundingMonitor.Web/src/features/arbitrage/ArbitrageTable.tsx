import React, { useState, useMemo, useCallback, useEffect, useRef } from "react";
import { paginateGroups } from "./arbitragePagination";
import { Spinner } from "../../shared/ui/Spinner";
import type { FundingArbitrageDto } from "../../types";
import {
  calcFundingRate,
  calcFundingSpread,
  getProfitability,
} from "./arbitrageTableModel";

interface ArbitrageTableProps {
  data: FundingArbitrageDto[];
  filtersKey?: string;
  isLoading?: boolean;
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void;
}

type SortColumn =
  | "aprSpread"
  | "priceSpreadPercent"
  | "fundingRateSpread"
  | "symbol";
type SortDirection = "asc" | "desc" | null;

interface SortConfig {
  column: SortColumn;
  direction: SortDirection;
}

interface SymbolGroup {
  symbol: string;
  best: FundingArbitrageDto;
  others: FundingArbitrageDto[];
}

const renderRow = (
  item: FundingArbitrageDto,
  key: string,
  showSymbol: boolean,
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void,
): React.ReactNode => {
  const isLongA = item.longExchange === item.exchangeA;
  const hasDirection = item.longExchange != null;
  const fundingRateA = calcFundingRate(item.fundingRateA);
  const fundingRateB = calcFundingRate(item.fundingRateB);

  const handleClick = () => {
    if (onArbitrageClick) {
      const symbol = item.symbol.replace("-USDT", "");
      onArbitrageClick(symbol, [item.exchangeA, item.exchangeB]);
    }
  };

  const symbolCell = showSymbol ? (
    <td
      className="px-4 py-3 text-center border-l align-middle"
      rowSpan={2}
      style={{ borderColor: "var(--tg-border)" }}
    >
      <span
        className="panel-value"
        style={{ color: "var(--tg-text)" }}
      >
        {item.symbol.replace("-USDT", "")}
      </span>
    </td>
  ) : (
    <td
      className="px-4 py-3 text-center border-l align-middle"
      rowSpan={2}
      style={{ borderColor: "var(--tg-border)" }}
    />
  );

  return (
    <React.Fragment key={key}>
      <tr
        className={`border-t transition-colors cursor-pointer`}
        onClick={handleClick}
        style={{ borderColor: "var(--tg-border)" }}
      >
        {symbolCell}
        {/* Биржа A */}
        <td
          className="px-4 py-2 border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <div className="flex items-center gap-2">
            <span
              className="w-8 shrink-0 text-left panel-caption"
              style={{
                color: isLongA ? "var(--tg-positive)" : "var(--tg-negative)",
              }}
            >
              {hasDirection ? (isLongA ? "Long" : "Short") : "—"}
            </span>
            <span
              className={`px-2 py-1 rounded-md panel-caption font-semibold ${getExchangeColorClass(item.exchangeA)}`}
            >
              {item.exchangeA}
            </span>
            <a
              href={item.exchangeAUrl}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="flex-shrink-0 inline-flex items-center"
              title={`Открыть ${item.exchangeA}`}
            >
              <svg
                className="w-3 h-3"
                style={{ color: "var(--tg-text-tertiary)" }}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
                />
              </svg>
            </a>
          </div>
        </td>
        {/* Цена A */}
        <td
          className="px-4 py-2 text-center border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <p
            className="panel-value"
            style={{ color: "var(--tg-text)" }}
          >
            $
            {item.priceA.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Спред цены — на две строки */}
        <td
          className="px-4 py-3 text-center border-l align-middle"
          rowSpan={2}
          style={{ borderColor: "var(--tg-border)" }}
        >
          <PriceSpread item={item} />
        </td>
        {/* Funding Rate A */}
        <td
          className="px-4 py-2 text-center border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <p
            className="panel-value"
            style={{
              color:
                fundingRateA > 0
                  ? "var(--tg-positive)"
                  : fundingRateA < 0
                    ? "var(--tg-negative)"
                    : "var(--tg-text-tertiary)",
            }}
          >
            {fundingRateA.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="panel-caption" style={{ color: "var(--tg-text-tertiary)" }}>
            {formatInterval(item.fundingIntervalHoursA, item.paymentsA)}
          </p>
        </td>
        {/* Спред фандинга — на две строки */}
        <td
          className="px-4 py-3 text-center border-l align-middle"
          rowSpan={2}
          style={{ borderColor: "var(--tg-border)" }}
        >
          {formatFundingSpread(item)}
        </td>
        <td
          className="px-4 py-3 text-center panel-value border-l align-middle"
          rowSpan={2}
          style={{ borderColor: "var(--tg-border)" }}
        >
          {formatApr(item.aprSpread)}
        </td>
      </tr>
      <tr
        className={`border-t transition-colors cursor-pointer`}
        onClick={handleClick}
        style={{ borderColor: "var(--tg-border)" }}
      >
        {/* Биржа B */}
        <td
          className="px-4 py-2 border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <div className="flex items-center gap-2">
            <span
              className="w-8 shrink-0 text-left panel-caption"
              style={{
                color: !isLongA ? "var(--tg-positive)" : "var(--tg-negative)",
              }}
            >
              {hasDirection ? (isLongA ? "Short" : "Long") : "—"}
            </span>
            <span
              className={`px-2 py-1 rounded-md panel-caption font-semibold ${getExchangeColorClass(item.exchangeB)}`}
            >
              {item.exchangeB}
            </span>
            <a
              href={item.exchangeBUrl}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="flex-shrink-0 inline-flex items-center"
              title={`Открыть ${item.exchangeB}`}
            >
              <svg
                className="w-3 h-3"
                style={{ color: "var(--tg-text-tertiary)" }}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
                />
              </svg>
            </a>
          </div>
        </td>
        {/* Цена B */}
        <td
          className="px-4 py-2 text-center border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <p
            className="panel-value"
            style={{ color: "var(--tg-text)" }}
          >
            $
            {item.priceB.toLocaleString(undefined, {
              minimumFractionDigits: 4,
              maximumFractionDigits: 8,
            })}
          </p>
        </td>
        {/* Funding Rate B */}
        <td
          className="px-4 py-2 text-center border-l"
          style={{ borderColor: "var(--tg-border)" }}
        >
          <p
            className="panel-value"
            style={{
              color:
                fundingRateB > 0
                  ? "var(--tg-positive)"
                  : fundingRateB < 0
                    ? "var(--tg-negative)"
                    : "var(--tg-text-tertiary)",
            }}
          >
            {fundingRateB.toLocaleString(undefined, {
              minimumFractionDigits: 0,
              maximumFractionDigits: 4,
            })}
            %
          </p>
          <p className="panel-caption" style={{ color: "var(--tg-text-tertiary)" }}>
            {formatInterval(item.fundingIntervalHoursB, item.paymentsB)}
          </p>
        </td>
      </tr>
    </React.Fragment>
  );
};

const getExchangeColorClass = (exchange: string): string => {
  return exchange === "Binance"
    ? "bg-[rgba(241,196,15,0.15)] text-[#F1C40F]"
    : exchange === "Bybit"
      ? "bg-[rgba(230,126,34,0.15)] text-[#E67E22]"
      : "bg-[var(--tg-bg-tertiary)] text-[var(--tg-text-secondary)]";
};

function formatInterval(hours: number | null, paymentsPerDay: number) {
  return hours !== null && hours > 0 ? `${hours} ч · ${paymentsPerDay} вып./день` : "—";
}

function formatFundingSpread(item: FundingArbitrageDto) {
  return `${calcFundingSpread(item).toLocaleString(undefined, { maximumFractionDigits: 4 })}%`;
}

function formatApr(apr: number | null) {
  return apr === null ? "—" : `${apr.toLocaleString(undefined, { maximumFractionDigits: 2 })}%`;
}

function PriceSpread({ item }: { item: FundingArbitrageDto }) {
  const percent = item.priceSpreadPercent;
  if (percent === null || item.shortExchange === null) return <>—</>;

  const amount = item.shortExchange === item.exchangeA ? item.priceSpread : -item.priceSpread;
  const sign = percent > 0 ? "+" : percent < 0 ? "-" : "";
  const color = percent > 0
    ? "var(--tg-positive)"
    : percent < 0 ? "var(--tg-negative)" : "var(--tg-text)";

  return (
    <>
      <p className="panel-value" style={{ color }}>
        {sign}{Math.abs(percent).toFixed(4)}%
      </p>
      <p className="panel-caption" style={{ color: "var(--tg-text-tertiary)" }}>
        {sign}${Math.abs(amount).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 8 })}
      </p>
    </>
  );
}

export const ArbitrageTable: React.FC<ArbitrageTableProps> = ({
  data,
  filtersKey = "",
  isLoading = false,
  onArbitrageClick,
}) => {
  const [sortConfig, setSortConfig] = useState<SortConfig>({
    column: "aprSpread",
    direction: "desc",
  });
  const [expandedSymbols, setExpandedSymbols] = useState<Set<string>>(
    new Set(),
  );
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [previousFiltersKey, setPreviousFiltersKey] = useState(filtersKey);
  const scrollRef = useRef<HTMLDivElement>(null);

  const handleSort = (column: SortColumn) => {
    setPage(1);
    setSortConfig((prev) => {
      if (prev.column === column) {
        if (prev.direction === "asc") return { column, direction: "desc" };
        if (prev.direction === "desc") return { column, direction: null };
        return { column, direction: "asc" };
      }
      return { column, direction: "asc" };
    });
  };

  const toggleExpand = useCallback((symbol: string) => {
    setExpandedSymbols((prev) => {
      const next = new Set(prev);
      if (next.has(symbol)) next.delete(symbol);
      else next.add(symbol);
      return next;
    });
  }, []);

  const compareRows = useCallback((a: FundingArbitrageDto, b: FundingArbitrageDto) => {
    if (!sortConfig.direction) return 0;

    const { column, direction } = sortConfig;
    const multiplier = direction === "asc" ? 1 : -1;

    let valueA: number | null;
    let valueB: number | null;

    if (column === "fundingRateSpread") {
      valueA = calcFundingSpread(a);
      valueB = calcFundingSpread(b);
    } else if (column === "aprSpread") {
      valueA = getProfitability(a);
      valueB = getProfitability(b);
    } else if (column === "priceSpreadPercent") {
      valueA = a.priceSpreadPercent;
      valueB = b.priceSpreadPercent;
    } else {
      return a.symbol.localeCompare(b.symbol) * multiplier;
    }

    if (valueA === null) return valueB === null ? 0 : 1;
    if (valueB === null) return -1;
    return (valueA - valueB) * multiplier;
  }, [sortConfig]);

  // Лучший APR определяет основную пару независимо от сортировки таблицы.
  const groupedData = useMemo(() => {
    const groups = new Map<string, FundingArbitrageDto[]>();
    for (const item of data) {
      const existing = groups.get(item.symbol);
      if (existing) existing.push(item);
      else groups.set(item.symbol, [item]);
    }

    const result: SymbolGroup[] = [];
    for (const [symbol, items] of groups) {
      const best = items.reduce((current, item) =>
        (item.aprSpread ?? -Infinity) > (current.aprSpread ?? -Infinity) ? item : current,
      );
      result.push({
        symbol,
        best,
        others: items.filter((item) => item !== best).sort(compareRows),
      });
    }
    return result.sort((a, b) => compareRows(a.best, b.best));
  }, [data, compareRows]);

  const pagination = paginateGroups(groupedData, page, pageSize);
  if (previousFiltersKey !== filtersKey) {
    setPreviousFiltersKey(filtersKey);
    setPage(1);
  } else if (pagination.page !== page) {
    setPage(pagination.page);
  }

  useEffect(() => {
    if (scrollRef.current) scrollRef.current.scrollTop = 0;
  }, [pagination.page, pageSize, filtersKey, sortConfig]);

  const SortIcon = ({ column }: { column: SortColumn }) => {
    if (sortConfig.column !== column || !sortConfig.direction) {
      return (
        <svg
          className="w-4 h-4 text-gray-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4"
          />
        </svg>
      );
    }
    if (sortConfig.direction === "asc") {
      return (
        <svg
          className="w-4 h-4 text-blue-400"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M5 15l7-7 7 7"
          />
        </svg>
      );
    }
    return (
      <svg
        className="w-4 h-4 text-blue-400"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M19 9l-7 7-7-7"
        />
      </svg>
    );
  };

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center" role="status">
        <div className="text-center text-[var(--tg-text-secondary)]">
          <Spinner className="mx-auto mb-4 h-12 w-12" />
          <p>Загрузка арбитражных данных...</p>
        </div>
      </div>
    );
  }

  if (groupedData.length === 0) {
    return (
      <div
        className="flex items-center justify-center h-full"
        style={{ color: "var(--tg-text-tertiary)" }}
      >
        <p>Нет арбитражных возможностей</p>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      <div ref={scrollRef} className="flex-1 min-h-0 overflow-auto [scrollbar-gutter:stable]">
        <table className="w-full table-fixed panel-text border-separate border-spacing-0 min-w-[1000px] [&_tbody_td]:border-b [&_tbody_td]:border-[var(--tg-border)] [&_td]:break-words">
          <colgroup>
            <col className="w-[12%]" />
            <col className="w-[18%]" />
            <col className="w-[13%]" />
            <col className="w-[14%]" />
            <col className="w-[18%]" />
            <col className="w-[14%]" />
            <col className="w-[11%]" />
          </colgroup>
          <thead style={{ backgroundColor: "var(--tg-bg-secondary)" }}>
            <tr>
              <th
                className="px-4 py-3 text-center font-medium border-b sticky top-0 z-20"
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Символ
              </th>
              <th
                className="px-4 py-3 text-center font-medium border-b sticky top-0 z-20"
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Биржи
              </th>
              <th
                className="px-4 py-3 text-center font-medium border-b sticky top-0 z-20"
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Цена
              </th>
              <th
                className="px-4 py-3 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
                onClick={() => handleSort("priceSpreadPercent")}
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span title="По ценам маркировки">Спред цены</span>
                  <SortIcon column="priceSpreadPercent" />
                </div>
              </th>
              <th
                className="px-4 py-3 text-center font-medium border-b sticky top-0 z-20"
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                Ставка финансирования
              </th>
              <th
                className="px-4 py-3 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
                onClick={() => handleSort("fundingRateSpread")}
                style={{
                  position: "sticky",
                  top: 0,
                  backgroundColor: "var(--tg-bg-secondary)",
                  color: "var(--tg-text-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-2">
                  <span>Спред фандинга</span>
                  <SortIcon column="fundingRateSpread" />
                </div>
              </th>
              <th
                scope="col"
                aria-sort={sortConfig.column === "aprSpread"
                  ? sortConfig.direction === "desc" ? "descending"
                    : sortConfig.direction === "asc" ? "ascending" : "none"
                  : undefined}
                className="px-4 py-3 text-center font-semibold border-b sticky top-0 z-20"
                style={{
                  backgroundColor: "var(--tg-bg-secondary)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <button
                  type="button"
                  className="mx-auto flex items-center gap-2"
                  onClick={() => handleSort("aprSpread")}
                >
                  APR
                  <SortIcon column="aprSpread" />
                </button>
              </th>
            </tr>
          </thead>
          <tbody>
            {pagination.items.map((group) => {
              const isExpanded = expandedSymbols.has(group.symbol);
              return (
                <React.Fragment key={group.symbol}>
                  {/* Лучшая связка */}
                  <React.Fragment>
                    <tr
                      className="border-t transition-colors cursor-pointer"
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                      style={{ borderColor: "var(--tg-border)" }}
                    >
                      {/* Пара + кнопка разворота */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <div className="flex items-center justify-center gap-1">
                          <span
                            className="panel-value"
                            style={{ color: "var(--tg-text)" }}
                          >
                            {group.best.symbol.replace("-USDT", "")}
                          </span>
                          {group.others.length > 0 && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                toggleExpand(group.symbol);
                              }}
                              className="p-0.5 rounded transition-colors"
                              style={{ backgroundColor: "transparent" }}
                            >
                              {isExpanded ? (
                                <svg
                                  className="w-3.5 h-3.5"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                  style={{ color: "var(--tg-text-secondary)" }}
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M5 15l7-7 7 7"
                                  />
                                </svg>
                              ) : (
                                <svg
                                  className="w-3.5 h-3.5"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                  style={{ color: "var(--tg-text-secondary)" }}
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M19 9l-7 7-7-7"
                                  />
                                </svg>
                              )}
                            </button>
                          )}
                        </div>
                      </td>
                      {/* Биржа A */}
                      <td
                        className="px-4 py-2 border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <div className="flex items-center gap-2">
                          <span
                            className="w-8 shrink-0 text-left panel-caption font-semibold"
                            style={{
                              color:
                                group.best.longExchange === group.best.exchangeA
                                  ? "var(--tg-positive)"
                                  : "var(--tg-negative)",
                            }}
                          >
                            {group.best.longExchange == null ? "—" :
                              group.best.longExchange === group.best.exchangeA ? "Long" : "Short"}
                          </span>
                          <span
                            className={`px-2 py-1 rounded-md panel-caption font-semibold ${getExchangeColorClass(group.best.exchangeA)}`}
                          >
                            {group.best.exchangeA}
                          </span>
                          <a
                            href={group.best.exchangeAUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="flex-shrink-0 inline-flex items-center"
                            title={`Открыть ${group.best.exchangeA}`}
                          >
                            <svg
                              className="w-3 h-3"
                              style={{ color: "var(--tg-text-tertiary)" }}
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
                              />
                            </svg>
                          </a>
                        </div>
                      </td>
                      {/* Цена A */}
                      <td
                        className="px-4 py-2 text-center border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <p
                          className="panel-value"
                          style={{ color: "var(--tg-text)" }}
                        >
                          $
                          {group.best.priceA.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Спред цены */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <PriceSpread item={group.best} />
                      </td>
                      {/* Funding Rate A */}
                      <td
                        className="px-4 py-2 text-center border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <p
                          className="panel-value"
                          style={{
                            color:
                              calcFundingRate(group.best.fundingRateA) > 0
                                ? "var(--tg-positive)"
                                : calcFundingRate(group.best.fundingRateA) < 0
                                  ? "var(--tg-negative)"
                                  : "var(--tg-text-tertiary)",
                          }}
                        >
                          {calcFundingRate(
                            group.best.fundingRateA,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p
                          className="panel-caption"
                          style={{ color: "var(--tg-text-tertiary)" }}
                        >
                          {formatInterval(group.best.fundingIntervalHoursA, group.best.paymentsA)}
                        </p>
                      </td>
                      {/* Спред фандинга */}
                      <td
                        className="px-4 py-3 text-center border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        {formatFundingSpread(group.best)}
                      </td>
                      <td
                        className="px-4 py-3 text-center panel-value border-l align-middle"
                        rowSpan={2}
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        {formatApr(group.best.aprSpread)}
                      </td>
                    </tr>
                    <tr
                      className="border-t transition-colors cursor-pointer"
                      onClick={() => {
                        if (onArbitrageClick) {
                          const symbol = group.best.symbol.replace("-USDT", "");
                          onArbitrageClick(symbol, [
                            group.best.exchangeA,
                            group.best.exchangeB,
                          ]);
                        }
                      }}
                      style={{ borderColor: "var(--tg-border)" }}
                    >
                      {/* Биржа B */}
                      <td
                        className="px-4 py-2 border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <div className="flex items-center gap-2">
                          <span
                            className="w-8 shrink-0 text-left panel-caption font-semibold"
                            style={{
                              color:
                                group.best.longExchange === group.best.exchangeA
                                  ? "var(--tg-negative)"
                                  : "var(--tg-positive)",
                            }}
                          >
                            {group.best.longExchange == null ? "—" :
                              group.best.longExchange === group.best.exchangeA ? "Short" : "Long"}
                          </span>
                          <span
                            className={`px-2 py-1 rounded-md panel-caption font-semibold ${getExchangeColorClass(group.best.exchangeB)}`}
                          >
                            {group.best.exchangeB}
                          </span>
                          <a
                            href={group.best.exchangeBUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={(e) => e.stopPropagation()}
                            className="flex-shrink-0 inline-flex items-center"
                            title={`Открыть ${group.best.exchangeB}`}
                          >
                            <svg
                              className="w-3 h-3"
                              style={{ color: "var(--tg-text-tertiary)" }}
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
                              />
                            </svg>
                          </a>
                        </div>
                      </td>
                      {/* Цена B */}
                      <td
                        className="px-4 py-2 text-center border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <p
                          className="panel-value"
                          style={{ color: "var(--tg-text)" }}
                        >
                          $
                          {group.best.priceB.toLocaleString(undefined, {
                            minimumFractionDigits: 4,
                            maximumFractionDigits: 8,
                          })}
                        </p>
                      </td>
                      {/* Funding Rate B */}
                      <td
                        className="px-4 py-2 text-center border-l"
                        style={{ borderColor: "var(--tg-border)" }}
                      >
                        <p
                          className="panel-value"
                          style={{
                            color:
                              calcFundingRate(group.best.fundingRateB) > 0
                                ? "var(--tg-positive)"
                                : calcFundingRate(group.best.fundingRateB) < 0
                                  ? "var(--tg-negative)"
                                  : "var(--tg-text-tertiary)",
                          }}
                        >
                          {calcFundingRate(
                            group.best.fundingRateB,
                          ).toLocaleString(undefined, {
                            minimumFractionDigits: 0,
                            maximumFractionDigits: 4,
                          })}
                          %
                        </p>
                        <p
                          className="panel-caption"
                          style={{ color: "var(--tg-text-tertiary)" }}
                        >
                          {formatInterval(group.best.fundingIntervalHoursB, group.best.paymentsB)}
                        </p>
                      </td>
                    </tr>
                  </React.Fragment>

                  {/* Дополнительные связки (при развороте) */}
                  {isExpanded &&
                    group.others.map((item, idx) =>
                      renderRow(
                        item,
                        `${item.symbol}-${item.exchangeA}-${item.exchangeB}-extra-${idx}`,
                        false,
                        onArbitrageClick,
                      ),
                    )}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
      <nav aria-label="Страницы арбитражных возможностей" className="mt-3 flex flex-wrap items-center justify-between gap-2 border-t border-[var(--tg-border)] pt-3 panel-text text-[var(--tg-text-secondary)]">
        <span>{pagination.start}–{pagination.end} из {groupedData.length} монет</span>
        <label className="flex items-center gap-2">
          <span>На странице</span>
          <select
            className="dashboard-control"
            value={pageSize}
            onChange={(event) => { setPageSize(Number(event.target.value)); setPage(1); }}
          >
            {[25, 50, 100].map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
        <div className="flex items-center gap-1">
          <button type="button" className="dashboard-control disabled:opacity-40" disabled={pagination.page === 1} onClick={() => setPage(1)} aria-label="Первая страница">«</button>
          <button type="button" className="dashboard-control disabled:opacity-40" disabled={pagination.page === 1} onClick={() => setPage(pagination.page - 1)}>Назад</button>
          <span className="px-2" aria-live="polite">{pagination.page} / {pagination.pageCount}</span>
          <button type="button" className="dashboard-control disabled:opacity-40" disabled={pagination.page === pagination.pageCount} onClick={() => setPage(pagination.page + 1)}>Вперёд</button>
          <button type="button" className="dashboard-control disabled:opacity-40" disabled={pagination.page === pagination.pageCount} onClick={() => setPage(pagination.pageCount)} aria-label="Последняя страница">»</button>
        </div>
      </nav>
    </div>
  );
};
