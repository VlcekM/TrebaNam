<script lang="ts">
	import { money } from '$lib/money';
	import type { CategorySpend } from '$lib/spending';

	let { slices, total }: { slices: CategorySpend[]; total: number } = $props();

	// Osem pevnych farieb; deviata a dalsia skupina je uz siva - viac odtienov by sa od seba
	// nedalo rozoznat a v legende pod grafom ma aj tak kazda svoj riadok so sumou.
	function sliceColor(slot: number) {
		return slot < 8 ? `var(--tn-viz-${slot + 1})` : 'var(--tn-faint)';
	}

	const R = 44;
	const C = 2 * Math.PI * R;

	// Kazdy vysek je jeden kruh s dash-array; medzera medzi vysekmi je kruh v farbe karty
	// nakresleny navrch, aby dve susedne farby nikdy nelezali priamo na sebe.
	const arcs = $derived.by(() => {
		let offset = 0;

		return slices.map((slice) => {
			const length = slice.share * C;
			const arc = { ...slice, dash: `${length} ${C - length}`, offset: -offset };
			offset += length;

			return arc;
		});
	});

	let hovered = $state<string>();
	const percent = new Intl.NumberFormat(undefined, { style: 'percent', maximumFractionDigits: 0 });
</script>

<div class="flex flex-col items-center gap-5 sm:flex-row sm:items-start sm:gap-8">
	<svg viewBox="0 0 120 120" class="size-44 flex-none" role="img" aria-hidden="true">
		<g transform="rotate(-90 60 60)">
			{#each arcs as arc (arc.code)}
				<circle
					cx="60"
					cy="60"
					r={R}
					fill="none"
					stroke={sliceColor(arc.slot)}
					stroke-width={hovered === arc.code ? 18 : 14}
					stroke-dasharray={arc.dash}
					stroke-dashoffset={arc.offset}
					class="transition-[stroke-width]"
					role="presentation"
					onmouseenter={() => (hovered = arc.code)}
					onmouseleave={() => (hovered = undefined)}
				>
					<title>{arc.label}: {money(arc.amount)}</title>
				</circle>
			{/each}
			{#if arcs.length > 1}
				{#each arcs as arc (arc.code)}
					<circle
						cx="60"
						cy="60"
						r={R}
						fill="none"
						stroke="var(--card)"
						stroke-width="20"
						stroke-dasharray="1.5 {C - 1.5}"
						stroke-dashoffset={arc.offset}
						pointer-events="none"
					/>
				{/each}
			{/if}
		</g>
		<text
			x="60"
			y="58"
			text-anchor="middle"
			class="fill-foreground text-[11px] font-bold"
			style="font-family: inherit"
		>
			{money(total)}
		</text>
	</svg>

	<ul class="flex w-full min-w-0 flex-1 flex-col">
		{#each slices as slice (slice.code)}
			<li
				class="flex items-center gap-3 border-t border-tn-muted/40 py-2 first:border-t-0 {hovered &&
				hovered !== slice.code
					? 'opacity-50'
					: ''}"
				onmouseenter={() => (hovered = slice.code)}
				onmouseleave={() => (hovered = undefined)}
			>
				<span class="size-2.5 flex-none rounded-full" style="background: {sliceColor(slice.slot)}"
				></span>
				<span class="min-w-0 flex-1 truncate text-[15px] font-bold">{slice.label}</span>
				<span class="text-[13px] text-tn-faint">{percent.format(slice.share)}</span>
				<span class="w-20 text-right text-[14px] font-bold tabular-nums">{money(slice.amount)}</span
				>
			</li>
		{/each}
	</ul>
</div>
