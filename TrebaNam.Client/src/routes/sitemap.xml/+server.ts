import type { RequestHandler } from './$types';

const SITE_URL = 'https://trebanam.martinvlcek.sk';

// Iba skutocne verejne stranky - /app/* je autentizovane SPA.
// Koncove lomitko vsade - trailingSlash je 'always', tak vyzeraju aj canonical odkazy.
const PATHS = ['/'];

export const prerender = true;

export const GET: RequestHandler = () => {
	const urls = PATHS.map((path) => `\t<url>\n\t\t<loc>${SITE_URL}${path}</loc>\n\t</url>`).join(
		'\n'
	);

	const body = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`;

	return new Response(body, { headers: { 'Content-Type': 'application/xml' } });
};
