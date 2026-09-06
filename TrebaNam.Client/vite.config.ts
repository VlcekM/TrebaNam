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
			// Appka je zatial len anglicka, takze ziadny jazyk v URL - paraglide drzime
			// kvoli katalogu retazcov, aby sa dal dalsi jazyk pridat bez prepisovania markupu.
			strategy: ['baseLocale']
		})
	],
	server: {
		// Pevny port - API v developmente proxuje prave sem.
		port: 3000
	}
});
