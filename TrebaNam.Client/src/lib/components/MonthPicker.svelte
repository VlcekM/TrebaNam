<script lang="ts">
	import ChevronDownIcon from '@lucide/svelte/icons/chevron-down';
	import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left';
	import ChevronRightIcon from '@lucide/svelte/icons/chevron-right';
	import { m } from '$lib/paraglide/messages.js';
	import { monthLabel, monthOf, shiftMonth, type MonthKey } from '$lib/spending';

	// Mesiac je bindable, aby oba smery - sipky tu a stlpec v grafe - hybali jednym stavom.
	// Sipky kracaju po jednom; nazov v strede je zaroven rozbalovacia ponuka, lebo do aprila
	// spred dvoch rokov sa nikomu nechce klepat dvadsatkrat.
	let {
		month = $bindable(),
		from,
		disabledNext = false,
		class: className = ''
	}: { month: MonthKey; from?: MonthKey; disabledNext?: boolean; class?: string } = $props();

	// Ponuka ide od prveho mesiaca s nakupom (alebo od vybraneho, ak je skorsi) po dnesny,
	// od najnovsieho, aby bol tento mesiac hore.
	const options = $derived.by(() => {
		const last = monthOf(new Date());
		let first = from && from < month ? from : month;
		if (first > last) first = last;

		const list: MonthKey[] = [];

		for (let key = last; key >= first; key = shiftMonth(key, -1)) list.push(key);

		return list;
	});

	const button =
		'inline-flex size-9 flex-none cursor-pointer items-center justify-center rounded-lg border border-tn-muted/40 bg-card text-tn-meta shadow-xs transition hover:bg-muted hover:text-foreground disabled:cursor-default disabled:opacity-40 disabled:hover:bg-card';
</script>

<div class="flex items-center gap-2 {className}">
	<button
		type="button"
		class={button}
		aria-label={m.month_previous()}
		onclick={() => (month = shiftMonth(month, -1))}
	>
		<ChevronLeftIcon class="size-4" />
	</button>

	<!-- Natívny select lezi priehladny cez nazov: vyzera ako text, klepnutie otvori ponuku. -->
	<label
		class="relative flex min-w-0 flex-1 cursor-pointer items-center justify-center gap-1 rounded-lg py-1.5 transition hover:bg-muted"
	>
		<span class="min-w-0 truncate text-[15px] font-bold capitalize">{monthLabel(month)}</span>
		<ChevronDownIcon class="size-4 flex-none text-tn-meta" />
		<select
			bind:value={month}
			aria-label={m.month_pick()}
			class="absolute inset-0 cursor-pointer opacity-0"
		>
			{#each options as key (key)}
				<option value={key} class="capitalize">{monthLabel(key)}</option>
			{/each}
		</select>
	</label>

	<button
		type="button"
		class={button}
		aria-label={m.month_next()}
		disabled={disabledNext}
		onclick={() => (month = shiftMonth(month, 1))}
	>
		<ChevronRightIcon class="size-4" />
	</button>
</div>
