const HIGHLIGHT_DURATION_MS = 2200;

export function scrollAndHighlight(elementId: string): void {
  setTimeout(() => {
    const el = document.getElementById(elementId);
    if (!el) return;

    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    el.classList.add('highlight-flash');
    setTimeout(() => el.classList.remove('highlight-flash'), HIGHLIGHT_DURATION_MS);
  }, 50);
}
