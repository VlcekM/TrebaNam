<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import GoogleIcon from '$lib/components/GoogleIcon.svelte';
	import Wordmark from '$lib/components/Wordmark.svelte';
	import { m } from '$lib/paraglide/messages.js';

	// Kym to nevieme, ponukame prihlasenie - to je pripad drvivej vacsiny navstev.
	let signedIn = $state(false);

	/**
	 * Bezi appka z ikony na ploche (PWA), alebo je to obycajna karta v prehliadaci?
	 * display-mode pokryva Android a desktop, navigator.standalone stary iOS.
	 */
	function launchedAsApp() {
		return (
			window.matchMedia('(display-mode: standalone)').matches ||
			(navigator as Navigator & { standalone?: boolean }).standalone === true
		);
	}

	// Stranka je predgenerovana, takze o prihlaseni sa da zistit az v prehliadaci -
	// cookie je HttpOnly a v HTML po nej niet stopy.
	onMount(async () => {
		// Kto klikol na ikonu appky, chce appku - nie uvodnu stranku. Ide tam rovno,
		// aj ked este prihlaseny nie je: /app si prihlasenie vypyta samo.
		// (Manifest ma start_url /app, toto je poistka pre starsie ikony na ploche.)
		if (launchedAsApp()) {
			await goto('/app', { replaceState: true });
			return;
		}

		// V prehliadaci nikoho nikam nehadzeme - len z tlacidla spravime pokracovanie.
		try {
			const res = await fetch('/api/auth/me', { credentials: 'include' });
			signedIn = res.ok;
		} catch {
			// Offline alebo nedostupne API - ostava prihlasenie, aj tak by nepreslo.
		}
	});
</script>

<svelte:head>
	<title>{m.home_page_title()}</title>
</svelte:head>

<!-- Verejna stranka je len prihlasenie - vsetko ostatne zije za /app. -->
<main class="flex min-h-[100svh] flex-col items-center justify-center px-4 py-16">
	<div
		class="flex w-full max-w-sm flex-col items-center gap-6 rounded-2xl border border-tn-muted/40 bg-card p-8 text-center shadow-xs"
	>
		<h1 class="text-[30px] leading-tight tracking-tight">
			<Wordmark />
		</h1>

		{#if signedIn}
			<!-- Bez ikony: text je dlhy a na uzkych telefonoch by sa lamal okolo nej. -->
			<a
				href="/app"
				class="inline-flex min-h-[50px] w-full items-center justify-center rounded-lg bg-tn-primary px-4 py-2 text-center leading-tight font-bold text-primary-foreground transition hover:bg-tn-primary-hover"
			>
				{m.home_continue()}
			</a>
		{:else}
			<!--
				Obycajny odkaz, nie fetch: /api/auth/login odpoveda 302 na Google a prehliadac
				musi presmerovanie nasledovat sam.
			-->
			<a
				href="/api/auth/login"
				data-sveltekit-reload
				class="inline-flex h-[50px] w-full items-center justify-center gap-3 rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover"
			>
				<GoogleIcon class="size-5" />
				{m.home_login_google()}
			</a>
		{/if}
	</div>
</main>
