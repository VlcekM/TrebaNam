<script lang="ts">
	import CloudOffIcon from '@lucide/svelte/icons/cloud-off';
	import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
	import TriangleAlertIcon from '@lucide/svelte/icons/triangle-alert';
	import { slide } from 'svelte/transition';
	import { rejectedCount, waitingCount } from '$lib/counts';
	import { ms } from '$lib/motion';
	import { sync } from '$lib/offline/state.svelte';
	import { m } from '$lib/paraglide/messages.js';

	// Prazdny riadok nema co zaberat miesto: lista je tam, len ked je co povedat.
	const shown = $derived(!sync.online || sync.pending > 0 || sync.rejected > 0);
</script>

{#if shown}
	<!--
		Jedna veta nad obsahom, nie dialog: bez signalu sa da appka pouzivat dalej a lista
		len hovori, ze sa zapisy zatial hromadia tu. Odmietnuty zapis je jedina vec, ktoru
		treba zavriet rukou - je to strata a ma sa na nu pozriet clovek.
	-->
	<div
		aria-live="polite"
		transition:slide={{ duration: ms(180) }}
		class="border-b border-tn-muted/40 {sync.rejected > 0 ? 'bg-destructive/10' : 'bg-muted'}"
	>
		<div
			class="mx-auto flex max-w-2xl items-center gap-2 px-4 py-2 text-[13px] font-bold lg:mx-0 lg:max-w-3xl lg:px-7"
		>
			{#if sync.rejected > 0}
				<TriangleAlertIcon class="size-4 flex-none text-destructive" />
				<span class="min-w-0 text-destructive">{rejectedCount(sync.rejected)}</span>

				<button
					type="button"
					onclick={() => (sync.rejected = 0)}
					class="ml-auto flex-none cursor-pointer rounded-lg px-2 py-1 text-tn-meta transition hover:bg-card"
				>
					{m.offline_rejected_dismiss()}
				</button>
			{:else if sync.sending}
				<RefreshCwIcon class="size-4 flex-none animate-spin text-tn-meta" />
				<span class="min-w-0 text-tn-meta">{m.offline_sending()}</span>
			{:else}
				<CloudOffIcon class="size-4 flex-none text-tn-meta" />

				<span class="min-w-0 text-tn-meta">
					{#if sync.pending > 0}
						{waitingCount(sync.pending)}
					{:else}
						{m.offline_title()}
					{/if}
				</span>
			{/if}
		</div>
	</div>
{/if}
