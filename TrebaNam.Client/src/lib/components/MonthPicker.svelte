<script lang="ts">
	import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left';
	import ChevronRightIcon from '@lucide/svelte/icons/chevron-right';
	import { m } from '$lib/paraglide/messages.js';
	import { monthLabel, shiftMonth, type MonthKey } from '$lib/spending';

	// Mesiac je bindable, aby oba smery - sipky tu a stlpec v grafe - hybali jednym stavom.
	let {
		month = $bindable(),
		disabledNext = false,
		class: className = ''
	}: { month: MonthKey; disabledNext?: boolean; class?: string } = $props();

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

	<span class="min-w-0 flex-1 truncate text-center text-[15px] font-bold capitalize">
		{monthLabel(month)}
	</span>

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
