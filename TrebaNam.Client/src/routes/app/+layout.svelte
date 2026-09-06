<script lang="ts">
	import AppHeader from '$lib/components/AppHeader.svelte';
	import AppSidebar from '$lib/components/AppSidebar.svelte';
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();
</script>

<!--
	Uzky displej je stlpec (hlavicka hore), siroky je handoffovy dvojstlpec:
	bocny panel vlavo, obsah vedla neho. min-h drzi pozadie aj pri malo obsahu.
-->
<div class="flex min-h-[100svh] flex-col lg:flex-row">
	<AppSidebar household={data.household} />

	<div class="flex min-w-0 flex-1 flex-col">
		<AppHeader household={data.household} />

		{@render children?.()}
	</div>
</div>
