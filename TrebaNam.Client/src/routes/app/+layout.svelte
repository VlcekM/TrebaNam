<script lang="ts">
	import AppHeader from '$lib/components/AppHeader.svelte';

	let { children } = $props();
</script>

<!--
	Stranka je stlpec cez cely displej: hlavicka hore, obsah (<main class="flex-1">)
	doplna zvysok, takze pozadie sedi aj ked je obsahu malo.
-->
<div class="flex min-h-[100svh] flex-col">
	<AppHeader />

	{@render children?.()}
</div>
