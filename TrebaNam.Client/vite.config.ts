import { paraglideVitePlugin } from '@inlang/paraglide-js';
import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit(),
		paraglideVitePlugin({
			project: './project.inlang',
			outdir: './src/lib/paraglide',
			// Jazyk drzi cookie, nie URL: appka bezi na jednej adrese, ktoru si ludia ulozia
			// na plochu, a preklapat ju na /sk by rozbilo aj ulozene odkazy, aj navrat z Googlu.
			// Bez cookie rozhoduje jazyk prehliadaca a az potom anglictina.
			strategy: ['cookie', 'preferredLanguage', 'baseLocale']
		})
	],
	server: {
		// Pevny port - API v developmente proxuje prave sem.
		port: 3000
	}
});
