<script lang="ts">
	import { money } from '$lib/money';
	import { monthShort, type MonthKey } from '$lib/spending';

	// Stlpec za mesiac; vybrany mesiac svieti akcentom a klepnutie na iny ho vyberie, takze
	// graf je zaroven druhy sposob, ako sa medzi mesiacmi pohybovat.
	let {
		months,
		selected = $bindable()
	}: { months: { month: MonthKey; spent: number }[]; selected: MonthKey } = $props();

	const max = $derived(Math.max(...months.map((entry) => entry.spent), 1));
</script>

<div class="flex h-40 items-end gap-1 sm:gap-2" role="group">
	{#each months as entry (entry.month)}
		{@const active = entry.month === selected}
		<button
			type="button"
			onclick={() => (selected = entry.month)}
			aria-pressed={active}
			aria-label="{monthShort(entry.month)}: {money(entry.spent)}"
			title="{monthShort(entry.month)}: {money(entry.spent)}"
			class="group flex h-full min-w-0 flex-1 cursor-pointer flex-col items-center justify-end gap-1.5 rounded-lg transition hover:bg-muted"
		>
			{#if active && entry.spent > 0}
				<span
					class="hidden text-[11px] font-bold whitespace-nowrap text-tn-primary-strong sm:block"
				>
					{money(entry.spent)}
				</span>
			{/if}
			<span
				class="w-full max-w-7 rounded-t transition-[height] {active
					? 'bg-tn-primary'
					: 'bg-tn-muted'}"
				style="height: {entry.spent > 0
					? Math.max((entry.spent / max) * 100, 3)
					: 0}%; min-height: {entry.spent > 0 ? '4px' : '2px'}"
			></span>
			<span
				class="text-[11px] capitalize {active
					? 'font-bold text-tn-primary-strong'
					: 'text-tn-faint'}"
			>
				{monthShort(entry.month)}
			</span>
		</button>
	{/each}
</div>
