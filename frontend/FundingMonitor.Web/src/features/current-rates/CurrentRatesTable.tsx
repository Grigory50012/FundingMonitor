import { useMemo, useState } from "react";
import { ExchangeBadge } from "../../entities/exchange/ExchangeBadge";
import {
  formatPercent,
  formatPrice,
  formatTime,
  getSignedColor,
} from "../../shared/lib/format";
import { nextSortDirection } from "../../shared/lib/sort";
import { EmptyState } from "../../shared/ui/EmptyState";
import { SortIcon } from "../../shared/ui/SortIcon";
import type { ExchangeType, FundingRateDto } from "../../types";
import {
  filterCurrentRates,
  sortCurrentRates,
} from "./currentRatesTableModel";
import type {
  CurrentRatesSortColumn,
  CurrentRatesSortConfig,
} from "./currentRatesTableModel";

interface CurrentRatesTableProps {
  data: FundingRateDto[];
  selectedExchanges: ExchangeType[];
}

type HeaderCellProps = {
  column: CurrentRatesSortColumn;
  label: string;
  sortConfig: CurrentRatesSortConfig;
  onSort: (column: CurrentRatesSortColumn) => void;
};

function HeaderCell({ column, label, sortConfig, onSort }: HeaderCellProps) {
  const direction = sortConfig.column === column ? sortConfig.direction : null;

  return (
    <th
      className="px-1.5 py-1.5 text-center font-medium border-b cursor-pointer transition-colors sticky top-0 z-20"
      onClick={() => onSort(column)}
      style={{
        position: "sticky",
        top: 0,
        backgroundColor: "var(--tg-bg-secondary)",
        color: "var(--tg-text-secondary)",
        borderColor: "var(--tg-border)",
      }}
    >
      <div className="flex items-center justify-center gap-0.5 flex-wrap">
        <span className="leading-tight">{label}</span>
        <SortIcon direction={direction} />
      </div>
    </th>
  );
}

export function CurrentRatesTable({
  data,
  selectedExchanges,
}: CurrentRatesTableProps) {
  const [sortConfig, setSortConfig] = useState<CurrentRatesSortConfig>({
    column: "fundingRate",
    direction: null,
  });

  const sortedData = useMemo(() => {
    const filteredData = filterCurrentRates(data, selectedExchanges);
    return sortCurrentRates(filteredData, sortConfig);
  }, [data, selectedExchanges, sortConfig]);

  const handleSort = (column: CurrentRatesSortColumn) => {
    setSortConfig((prev) => ({
      column,
      direction:
        prev.column === column ? nextSortDirection(prev.direction) : "asc",
    }));
  };

  if (sortedData.length === 0) {
    return (
      <EmptyState>
        {"\u041d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445 \u0434\u043b\u044f \u043e\u0442\u043e\u0431\u0440\u0430\u0436\u0435\u043d\u0438\u044f"}
      </EmptyState>
    );
  }

  return (
    <div className="h-full overflow-y-auto overflow-x-hidden min-w-0">
      <table className="w-full table-fixed text-xs border-separate border-spacing-0 [&_tbody_td]:border-b [&_tbody_td]:border-[var(--tg-border)]">
        <colgroup>
          <col className="w-[20%]" />
          <col className="w-[22%]" />
          <col className="w-[26%]" />
          <col className="w-[14%]" />
          <col className="w-[18%]" />
        </colgroup>
        <thead style={{ backgroundColor: "var(--tg-bg-secondary)" }}>
          <tr>
            <th
              className="px-1.5 py-1.5 text-center font-medium sticky top-0 left-0 z-30 border-b"
              style={{
                position: "sticky",
                top: 0,
                backgroundColor: "var(--tg-bg-secondary)",
                color: "var(--tg-text-secondary)",
                borderColor: "var(--tg-border)",
              }}
            >
              {"\u0411\u0438\u0440\u0436\u0430"}
            </th>
            <HeaderCell
              column="markPrice"
              label={"\u041c\u0430\u0440\u043a."}
              sortConfig={sortConfig}
              onSort={handleSort}
            />
            <HeaderCell
              column="fundingRate"
              label={"\u0421\u0442\u0430\u0432\u043a\u0430"}
              sortConfig={sortConfig}
              onSort={handleSort}
            />
            <HeaderCell
              column="nextFundingTime"
              label={"\u0412\u0440\u0435\u043c\u044f"}
              sortConfig={sortConfig}
              onSort={handleSort}
            />
            <HeaderCell
              column="apr"
              label="APR"
              sortConfig={sortConfig}
              onSort={handleSort}
            />
          </tr>
        </thead>
        <tbody>
          {sortedData.map((item) => (
            <tr
              key={`${item.exchange}-${item.symbol}`}
              className="border-t transition-colors"
              style={{ borderColor: "var(--tg-border)" }}
            >
              <td
                className="px-1.5 py-2 font-medium sticky left-0 z-10 border-r align-middle text-center"
                style={{
                  backgroundColor: "var(--tg-bg)",
                  color: "var(--tg-text)",
                  borderColor: "var(--tg-border)",
                }}
              >
                <div className="flex items-center justify-center gap-1 min-w-0">
                  <ExchangeBadge exchange={item.exchange} compact />
                  <a
                    href={item.exchangeUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    onClick={(event) => event.stopPropagation()}
                    className="flex-shrink-0 inline-flex items-center"
                    title={`\u041e\u0442\u043a\u0440\u044b\u0442\u044c ${item.exchange}`}
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
              <td
                className="px-1.5 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="font-semibold text-[11px] tabular-nums leading-tight break-all text-center"
                  style={{ color: "var(--tg-text)" }}
                >
                  {formatPrice(item.markPrice, {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 8,
                  })}
                </p>
              </td>
              <td
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <div className="flex flex-col items-center justify-center gap-0.5 leading-tight">
                  <p
                    className="text-xs font-bold tabular-nums"
                    style={{ color: getSignedColor(item.fundingRate) }}
                  >
                    {formatPercent(item.fundingRate * 100, {
                      minimumFractionDigits: 3,
                      maximumFractionDigits: 3,
                    })}
                  </p>
                  <p
                    className="text-[10px] leading-none"
                    style={{ color: "var(--tg-text-tertiary)" }}
                  >
                    {item.numberOfPaymentsPerDay}{" "}
                    {"\u0432\u044b\u043f./\u0434\u0435\u043d\u044c"}
                  </p>
                </div>
              </td>
              <td
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="font-medium text-[11px] tabular-nums text-center"
                  style={{ color: "var(--tg-text)" }}
                >
                  {formatTime(item.nextFundingTime)}
                </p>
              </td>
              <td
                className="px-1 py-2 text-center border-l align-middle"
                style={{ borderColor: "var(--tg-border)" }}
              >
                <p
                  className="text-xs font-bold tabular-nums text-center mx-auto"
                  style={{ color: getSignedColor(item.apr) }}
                >
                  {formatPercent(item.apr, {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  })}
                </p>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
