<script lang="ts">
	import { m } from '$lib/paraglide/messages.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();
</script>

<svelte:head>
	<title>{m.buy_page_title()}</title>
</svelte:head>

<!-- Zatial len prazdna obrazovka; polozky pridame, ked bude co na nu vesat. -->
<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<div class="flex flex-col gap-1 px-1.5">
			<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
				{m.buy_title()}
			</h1>
			<p class="text-sm text-tn-meta">{data.household?.name}</p>
		</div>

		<section class="rounded-2xl border border-tn-muted/40 bg-card px-6 py-12 text-center shadow-xs">
			<p class="text-[15px] leading-relaxed text-muted-foreground">{m.buy_empty()}</p>
		</section>
	</div>
</main>
