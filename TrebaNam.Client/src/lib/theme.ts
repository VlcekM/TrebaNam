export type Theme = 'light' | 'dark';

export const THEME_COOKIE = 'theme';

/** Tema pre navstevnika bez cookie. Drz ju v synchro s inline skriptom v app.html. */
export const DEFAULT_THEME: Theme = 'light';

const THEME_MAX_AGE = 60 * 60 * 24 * 365; // 1 rok

/**
 * Vytiahne temu z cookie stringu. Pouziva sa aj pri prerenderi (hooks.server.ts),
 * aj v inline skripte v app.html - drz tu regexy v synchro.
 */
export function parseTheme(cookieHeader: string | null | undefined): Theme | null {
	const match = cookieHeader?.match(new RegExp(`(?:^|;\\s*)${THEME_COOKIE}=(light|dark)`));
	return match ? (match[1] as Theme) : null;
}

/** Aktualna tema podla <html class="dark">, ktoru nastavil inline skript / prerender. */
export function getTheme(): Theme {
	return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
}

export function setTheme(theme: Theme) {
	const root = document.documentElement;

	root.classList.toggle('dark', theme === 'dark');
	root.style.colorScheme = theme;

	document.cookie = `${THEME_COOKIE}=${theme}; path=/; max-age=${THEME_MAX_AGE}; SameSite=Lax`;
}

export function toggleTheme() {
	setTheme(getTheme() === 'dark' ? 'light' : 'dark');
}
