type SpinnerProps = {
  className?: string;
};

export function Spinner({ className = "w-12 h-12" }: SpinnerProps) {
  return (
    <div
      className={`${className} border-4 rounded-full animate-spin`}
      style={{
        borderColor: "var(--tg-border)",
        borderTopColor: "var(--tg-button)",
      }}
    />
  );
}
