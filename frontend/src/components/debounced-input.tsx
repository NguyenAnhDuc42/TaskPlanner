import * as React from "react";
import { cn } from "@/lib/utils";

export interface DebouncedInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "onChange"> {
  value: string;
  onChange: (value: string) => void;
  debounceMs?: number;
}

export const DebouncedInput = React.forwardRef<HTMLInputElement, DebouncedInputProps>(
  ({ value: initialValue, onChange, debounceMs = 800, className, ...props }, ref) => {
    const [value, setValue] = React.useState(initialValue);
    const timeoutRef = React.useRef<ReturnType<typeof setTimeout> | null>(null);

    React.useEffect(() => {
      setValue(initialValue);
    }, [initialValue]);

    const flush = React.useCallback(() => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
        timeoutRef.current = null;
        onChange(value);
      }
    }, [onChange, value]);

    const flushRef = React.useRef(flush);
    React.useEffect(() => {
      flushRef.current = flush;
    }, [flush]);

    React.useEffect(() => {
      return () => {
        flushRef.current();
      };
    }, []);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newValue = e.target.value;
      setValue(newValue);

      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }

      timeoutRef.current = setTimeout(() => {
        timeoutRef.current = null;
        onChange(newValue);
      }, debounceMs);
    };

    const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
      flush();
      if (props.onBlur) {
        props.onBlur(e);
      }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter") {
        flush();
        e.currentTarget.blur();
      }
      if (props.onKeyDown) {
        props.onKeyDown(e);
      }
    };

    return (
      <input
        {...props}
        ref={ref}
        value={value}
        onChange={handleChange}
        onBlur={handleBlur}
        onKeyDown={handleKeyDown}
        className={cn(className)}
      />
    );
  }
);

DebouncedInput.displayName = "DebouncedInput";
