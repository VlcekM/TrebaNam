<script lang="ts">
	import CategoryDonut from '$lib/components/CategoryDonut.svelte';
	import MonthBars from '$lib/components/MonthBars.svelte';
	import MonthPicker from '$lib/components/MonthPicker.svelte';
	import { withoutTotalCount } from '$lib/counts';
	import { money } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import { monthOf, monthlyTotals, spendByCategory, totals, tripsInMonth } from '$lib/spending';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const thisMonth = monthOf(new Date());
	let month = $state(thisMonth);

	const categories = $derived(data.household?.categories ?? []);
	const trips = $derived(tripsInMonth(data.records, month));
	const sum = $derived(totals(trips));
	const slices = $derived(spendByCategory(trips, categories));
	// Dvanast mesiacov po vybrany; dalej ako do tohto mesiaca sa ist neda, tam este nic nie je.
	const months = $derived(monthlyTotals(data.records, month, 12));
</script>

<svelte:head>
	<title>{m.spending_page_title()}</title>
</svelte:head>

<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<div class="flex flex-col gap-1 px-1.5">
			<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
				{m.spending_title()}
			</h1>
			<p class="text-sm text-tn-meta">{data.household?.name}</p>
		</div>

		<MonthPicker bind:month disabledNext={month >= thisMonth} class="px-1.5" />

		<section class="flex flex-col gap-4 rounded-2xl bg-tn-tint p-6">
			<p class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.spending_month_total()}
			</p>
			<p class="text-[30px] leading-none font-bold tracking-tight text-tn-primary-strong">
				{money(sum.spent)}
			</p>
			{#if sum.withoutTotal > 0}
				<p class="text-[13px] text-tn-meta">{withoutTotalCount(sum.withoutTotal)}</p>
			{/if}
		</section>

		<section
			class="flex flex-col gap-4 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs"
		>
			<h2 class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.spending_by_category()}
			</h2>

			{#if slices.length > 0}
				<CategoryDonut {slices} total={sum.spent} />
				<p class="text-[13px] text-tn-faint">{m.spending_hint()}</p>
			{:else}
				<p class="text-[15px] leading-relaxed text-muted-foreground">
					{sum.trips > 0 ? m.spending_empty() : m.spending_no_data()}
				</p>
			{/if}
		</section>

		<section
			class="flex flex-col gap-4 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs"
		>
			<h2 class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.spending_by_month()}
			</h2>
			<MonthBars {months} bind:selected={month} />
		</section>
	</div>
</main>
