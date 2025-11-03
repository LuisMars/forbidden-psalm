let dotNetHelper = null;
let escKeyHandler = null;
let popStateHandler = null;
let historyStatePushed = false;

export function setupModalHandlers(dotNetRef) {
    dotNetHelper = dotNetRef;

    // ESC key handler
    escKeyHandler = (event) => {
        if (event.key === 'Escape' || event.key === 'Esc') {
            event.preventDefault();
            dotNetHelper.invokeMethodAsync('CloseFromJS');
        }
    };
    document.addEventListener('keydown', escKeyHandler);

    // Push a history state for mobile back button
    if (!historyStatePushed) {
        window.history.pushState({ modal: true }, '');
        historyStatePushed = true;
    }

    // Back button handler
    popStateHandler = (event) => {
        if (historyStatePushed) {
            dotNetHelper.invokeMethodAsync('CloseFromJS');
            historyStatePushed = false;
        }
    };
    window.addEventListener('popstate', popStateHandler);
}

export function cleanupModalHandlers() {
    // Remove ESC key handler
    if (escKeyHandler) {
        document.removeEventListener('keydown', escKeyHandler);
        escKeyHandler = null;
    }

    // Remove popstate handler
    if (popStateHandler) {
        window.removeEventListener('popstate', popStateHandler);
        popStateHandler = null;
    }

    // Clean up history state if we pushed one
    if (historyStatePushed) {
        try {
            // Go back to remove the modal state we pushed
            // But prevent the popstate from triggering close again
            window.removeEventListener('popstate', popStateHandler);
            window.history.back();
            historyStatePushed = false;
        } catch (e) {
            // Ignore errors
        }
    }

    dotNetHelper = null;
}
