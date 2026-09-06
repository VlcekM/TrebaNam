import type { Handle } from '@sveltejs/kit';
import { DEFAULT_THEME, parseTheme } from '$lib/theme';

// Bezi len pocas prerenderu - za behu ziadny SvelteKit server neexistuje.
export const handle: Handle = ({ event, resolve }) => {
	// Temu doplname uz do HTML, aby prvy render sedel s cookie.
	const theme = parseTheme(event.request.headers.get('cookie')) ?? DEFAULT_THEME;

	return resolve(event, {
		transformPageChunk: ({ html }) =>
			html.replace('%trebanam.theme%', theme === 'dark' ? 'dark' : '')
	});
};
