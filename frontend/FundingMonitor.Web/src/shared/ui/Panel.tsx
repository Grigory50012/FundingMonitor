import type { ReactNode } from "react";

type PanelProps = {
  children: ReactNode;
  className?: string;
};

export function Panel({ children, className = "" }: PanelProps) {
  return (
    <section
      className={`rounded-2xl overflow-hidden ${className}`}
      style={{
        backgroundColor: "var(--tg-bg-secondary)",
        border: "1px solid var(--tg-border)",
      }}
    >
      {children}
    </section>
  );
}
