import type { ReactNode } from "react";

type EmptyStateProps = {
  children: ReactNode;
  tone?: "muted" | "danger";
  className?: string;
};

export function EmptyState({
  children,
  tone = "muted",
  className = "",
}: EmptyStateProps) {
  return (
    <div
      className={`flex items-center justify-center h-full ${className}`}
      style={{
        color:
          tone === "danger" ? "var(--tg-negative)" : "var(--tg-text-tertiary)",
      }}
    >
      <p className="text-xs">{children}</p>
    </div>
  );
}
