<script lang="ts">
	import AppHeader from '$lib/components/AppHeader.svelte';
	import AppSidebar from '$lib/components/AppSidebar.svelte';
	import { connectHousehold } from '$lib/realtime';
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();

	// Spojenie visi na domacnosti, nie na jej obsahu - kazde nacitanie prinesie novy objekt
	// a podla neho by sa hub odpajal a pripajal dokola.
	const householdID = $derived(data.household?.id);

	// Zoznam je spolocny, tak ma byt spolocny aj naraz: kym je clovek v domacnosti, pocuvame
	// hub a po kazdej zmene si obrazovky nacitaju svoje data znova.
	$effect(() => {
		if (!householdID) return;

		return connectHousehold();
	});
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
