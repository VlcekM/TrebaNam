<script lang="ts">
	import { m } from '$lib/paraglide/messages.js';
	import { parseMoney } from '$lib/money';

	// Cena riadku z blocku, opravitelna rukou - zlava, ktoru pokladna nevypisala, alebo kus
	// zaplateny inak. Rozdiel sa hlasi von, aby sa o neho posunula aj suma nakupu.
	let {
		price = $bindable(0),
		name,
		onshift
	}: {
		price?: number;
		name: string;
		onshift?: (delta: number) => void;
	} = $props();

	let text = $state(price.toFixed(2));

	function commit() {
		const value = parseMoney(text);

		if (value === undefined) {
			text = price.toFixed(2);
			return;
		}

		const rounded = Math.round(value * 100) / 100;
		const delta = rounded - price;

		price = rounded;
		text = rounded.toFixed(2);

		if (delta !== 0) onshift?.(delta);
	}
</script>

<span
	class="flex h-10 w-20 flex-none items-center gap-1 rounded-lg border border-input bg-background px-2 focus-within:border-tn-primary"
>
	<span class="flex-none text-[12px] font-bold text-tn-faint">€</span>
	<input
		bind:value={text}
		onchange={commit}
		inputmode="decimal"
		maxlength={10}
		aria-label={m.receipt_line_price({ name })}
		class="h-full min-w-0 flex-1 bg-transparent text-right text-[13px] font-semibold outline-none"
	/>
</span>
