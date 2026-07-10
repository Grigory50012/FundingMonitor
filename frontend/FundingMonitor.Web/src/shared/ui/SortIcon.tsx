import type { SortDirection } from "../lib/sort";

type SortIconProps = {
  direction: SortDirection;
  className?: string;
};

export function SortIcon({
  direction,
  className = "w-3 h-3 flex-shrink-0",
}: SortIconProps) {
  if (direction === "asc") {
    return (
      <svg
        className={`${className} text-blue-400`}
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

  if (direction === "desc") {
    return (
      <svg
        className={`${className} text-blue-400`}
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
  }

  return (
    <svg
      className={`${className} text-gray-600`}
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
