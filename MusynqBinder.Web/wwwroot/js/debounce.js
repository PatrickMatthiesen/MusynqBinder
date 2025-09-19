// Generic client-side debounce for Blazor Server inputs.
// Minimizes SignalR traffic by only invoking .NET after user stops typing.
// Usage: dynamic import then initDebounce(element, dotNetRef, delayMs)

export function initDebounce(element, dotNetRef, delay) {
    if (!element) return;
    let timerId = null;

    const handler = () => {
        if (timerId) clearTimeout(timerId);
        timerId = setTimeout(() => {
            // Trailing-edge invoke
            dotNetRef.invokeMethodAsync('OnDebouncedInput', element.value);
        }, delay);
    };

    element.__debounceHandler = handler; // store reference for optional cleanup
    element.addEventListener('input', handler);
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
