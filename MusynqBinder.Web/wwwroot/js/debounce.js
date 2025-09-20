// Generic client-side debounce for Blazor Server inputs.

export function initDebounce(element, dotNetRef, delay) {
    if (!element) return;
    let timerId = null;

    const handler = () => {
        if (timerId) clearTimeout(timerId);
        timerId = setTimeout(() => {
            dotNetRef.invokeMethodAsync('OnDebouncedInput', element.value);
        }, delay);
    };

    element.__debounceHandler = handler;
    element.addEventListener('input', handler);
    element.__debounceTimerRef = () => timerId;
}

export function disposeDebounce(element) {
    if (!element) return;
    if (element.__debounceHandler) {
        element.removeEventListener('input', element.__debounceHandler);
        delete element.__debounceHandler;
    }
}

export function setInputValue(element, value) {
    if (!element) return;
    if (element.value !== value) {
        element.value = value ?? '';
    }
}

// Flush any pending debounce immediately (used when user presses Enter early)
export function flushDebounce(element, dotNetRef) {
    if (!element) return;
    dotNetRef.invokeMethodAsync('OnDebouncedInput', element.value);
}
