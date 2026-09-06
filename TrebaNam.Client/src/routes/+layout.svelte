<script lang="ts">
	import '../app.css';
	import { page } from '$app/state';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';
	import { getLocale } from '$lib/paraglide/runtime';

	// Jedina domena, pod ktorou stranka bezi - staci na canonical.
	const SITE_URL = 'https://trebanam.martinvlcek.sk';

	let { children } = $props();

	// V /app je prepinac sucastou lepivej hlavicky, tu by sa s nou prekryval.
	const inApp = $derived(page.url.pathname.startsWith('/app'));

	$effect(() => {
		document.documentElement.lang = getLocale();
	});
</script>

<svelte:head>
	{#if !inApp}
		<link rel="canonical" href="{SITE_URL}{page.url.pathname}" />
	{/if}
</svelte:head>

{#if !inApp}
	<ThemeToggle
		class="fixed top-[max(1rem,env(safe-area-inset-top))] right-[max(1rem,env(safe-area-inset-right))] z-50"
	/>
{/if}

{@render children?.()}
