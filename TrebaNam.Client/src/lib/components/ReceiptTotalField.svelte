<script lang="ts">
	import { m } from '$lib/paraglide/messages.js';

	// Suma z blocku je len navrh: zlava pri pokladni, vratena zaloha alebo kus zaplateny
	// zvlast znamenaju, ze zaplatene bolo nieco ine. Prazdne pole znamena nakup bez sumy.
	let {
		amount = $bindable(''),
		invalid = false
	}: {
		amount?: string;
		invalid?: boolean;
	} = $props();
</script>

<label class="flex flex-col gap-1">
	<span class="flex items-center gap-3">
		<span class="flex-none text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
			{m.trip_total_label()}
		</span>
		<span
			class="flex h-10 min-w-0 flex-1 items-center gap-2 rounded-lg border bg-background px-3 focus-within:border-tn-primary {invalid
				? 'border-destructive'
				: 'border-input'}"
		>
			<span class="flex-none font-bold text-tn-meta">€</span>
			<input
				bind:value={amount}
				inputmode="decimal"
				maxlength={12}
				placeholder={m.trip_total_placeholder()}
				class="h-full min-w-0 flex-1 bg-transparent font-bold text-tn-primary-strong outline-none"
			/>
		</span>
	</span>

	{#if invalid}
		<span class="text-sm font-semibold text-destructive">{m.trip_total_invalid()}</span>
	{/if}
</label>
