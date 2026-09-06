<script lang="ts">
	import GoogleIcon from '$lib/components/GoogleIcon.svelte';
	import { m } from '$lib/paraglide/messages.js';
</script>

<svelte:head>
	<title>{m.home_page_title()}</title>
</svelte:head>

<!-- Verejna stranka je len prihlasenie - vsetko ostatne zije za /app. -->
<main class="flex min-h-[100svh] flex-col items-center justify-center px-4 py-16">
	<div
		class="flex w-full max-w-sm flex-col items-center gap-6 rounded-2xl border border-tn-muted/40 bg-card p-8 text-center shadow-xs"
	>
		<h1 class="text-[30px] leading-tight font-bold tracking-tight">
			{m.brand_name()}
		</h1>

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
	</div>
</main>
