// Theme Switcher for Forbidden Psalm Builder
// Manages both core theme (morkborg, default, modern, elegant) and color mode (light, dark)

(function () {
    'use strict';

    const STORAGE_THEME_KEY = 'forbiddenpsalm-theme';
    const STORAGE_COLOR_MODE_KEY = 'forbiddenpsalm-color-mode';
    const DEFAULT_THEME = 'morkborg';
    const DEFAULT_COLOR_MODE = 'dark';

    // Storage functions
    function getStoredTheme() {
        return localStorage.getItem(STORAGE_THEME_KEY);
    }

    function setStoredTheme(theme) {
        localStorage.setItem(STORAGE_THEME_KEY, theme);
    }

    function getStoredColorMode() {
        return localStorage.getItem(STORAGE_COLOR_MODE_KEY);
    }

    function setStoredColorMode(colorMode) {
        localStorage.setItem(STORAGE_COLOR_MODE_KEY, colorMode);
    }

    // Get preferred theme (from storage or default)
    function getPreferredTheme() {
        const storedTheme = getStoredTheme();
        if (storedTheme) {
            return storedTheme;
        }
        return DEFAULT_THEME;
    }

    // Get preferred color mode (from storage or default)
    function getPreferredColorMode() {
        const storedColorMode = getStoredColorMode();
        if (storedColorMode) {
            return storedColorMode;
        }
        return DEFAULT_COLOR_MODE;
    }

    // Apply theme to document
    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-core', theme);
        updateActiveThemeButton(theme);
    }

    // Apply color mode to document
    function setColorMode(colorMode) {
        if (colorMode === 'light') {
            document.documentElement.setAttribute('data-bs-theme', 'light');
        } else {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
        }
        updateColorModeButton(colorMode);
    }

    // Update active state on theme selector
    function updateActiveThemeButton(theme) {
        const selector = document.getElementById('theme-selector');
        if (selector) {
            selector.value = theme;
        }
    }

    // Update color mode toggle button icon and text
    function updateColorModeButton(colorMode) {
        const button = document.getElementById('color-mode-toggle');
        const text = document.getElementById('color-mode-text');
        if (button) {
            const icon = button.querySelector('i');
            if (colorMode === 'light') {
                if (icon) icon.className = 'fas fa-moon';
                if (text) text.textContent = 'Dark';
                button.setAttribute('aria-label', 'Switch to dark mode');
            } else {
                if (icon) icon.className = 'fas fa-sun';
                if (text) text.textContent = 'Light';
                button.setAttribute('aria-label', 'Switch to light mode');
            }
        }
    }

    // Handle theme change
    function handleThemeChange(theme) {
        setStoredTheme(theme);
        setTheme(theme);
    }

    // Handle color mode toggle
    function handleColorModeToggle() {
        const currentColorMode = document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
        const newColorMode = currentColorMode === 'light' ? 'dark' : 'light';
        setStoredColorMode(newColorMode);
        setColorMode(newColorMode);
    }

    // Initialize theme on page load
    function initializeTheme() {
        const preferredTheme = getPreferredTheme();
        const preferredColorMode = getPreferredColorMode();

        setTheme(preferredTheme);
        setColorMode(preferredColorMode);
    }

    // Set up event listeners
    function setupEventListeners() {
        // Theme selector dropdown
        const themeSelector = document.getElementById('theme-selector');
        if (themeSelector) {
            themeSelector.addEventListener('change', function(e) {
                handleThemeChange(e.target.value);
            });
        }

        // Color mode toggle button
        const colorModeToggle = document.getElementById('color-mode-toggle');
        if (colorModeToggle) {
            colorModeToggle.addEventListener('click', handleColorModeToggle);
        }
    }

    // Initialize theme immediately to prevent flicker
    initializeTheme();

    // Set up event listeners after Blazor has rendered
    // Use a MutationObserver to watch for the footer being added to the DOM
    function waitForElements() {
        const themeSelector = document.getElementById('theme-selector');
        const colorModeToggle = document.getElementById('color-mode-toggle');

        if (themeSelector && colorModeToggle) {
            // Update UI to reflect current state
            const currentTheme = document.documentElement.getAttribute('data-bs-core') || DEFAULT_THEME;
            const currentColorMode = document.documentElement.getAttribute('data-bs-theme') === 'light' ? 'light' : 'dark';
            updateActiveThemeButton(currentTheme);
            updateColorModeButton(currentColorMode);

            // Set up event listeners
            setupEventListeners();
        } else {
            // If elements don't exist yet, wait and try again
            setTimeout(waitForElements, 100);
        }
    }

    // Initialize on DOM content loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            waitForElements();
        });
    } else {
        // DOM already loaded
        waitForElements();
    }

})();

// File Download Helper
window.downloadFile = function(filename, content) {
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    window.URL.revokeObjectURL(url);
};
