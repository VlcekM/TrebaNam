<script lang="ts">
	import { goto, invalidateAll } from '$app/navigation';
	import { parseMoney } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import { finishShopping, setTripTotal } from '$lib/shopping';
	import type { ShoppingRecord } from '$lib/types';

	// Bez zaznamu sa nakup prave ukoncuje, so zaznamom sa oprava suma uz ukonceneho. Pole je
	// v oboch pripadoch to iste, takze je to jeden dialog a nie dva takmer rovnake. Pri ukonceni
	// treba vediet aj to, ktory zoznam sa donakupil - nakupuje sa po zoznamoch.
	let {
		open = $bindable(false),
		record,
		listID
	}: { open?: boolean; record?: ShoppingRecord; listID?: string } = $props();

	let dialog = $state<HTMLDialogElement>();
	let amount = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let invalid = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			// Na centy, nech sa opravena suma cita rovnako, ako sa zobrazuje.
			amount = record?.totalCost != null ? record.totalCost.toFixed(2) : '';
			failed = false;
			invalid = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function save(total?: number) {
		if (pending) return;

		pending = true;
		failed = false;

		try {
			if (record) {
				await setTripTotal(record.id, total);
			} else if (listID) {
				await finishShopping(listID, total);
				// Nakup skoncil a jeho polozky su uz len v historii - tam sa clovek aj pozrie.
				await goto('/app/history');
			}

			await invalidateAll();
			open = false;
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
	}

	function submit(event: SubmitEvent) {
		event.preventDefault();

		const total = parseMoney(amount);

		// Prazdne pole ma vlastne tlacidlo, takze sem sa dostane len napisany nezmysel.
		if (total === undefined) {
			invalid = true;
			return;
		}

		invalid = false;
		save(total);
	}

	function backdropDown(event: MouseEvent) {
		pressedBackdrop = event.target === dialog;
	}

	function backdropClick(event: MouseEvent) {
		if (pressedBackdrop && event.target === dialog && !pending) {
			open = false;
		}
	}
</script>

<dialog
	bind:this={dialog}
	onclose={() => (open = false)}
	onmousedown={backdropDown}
	onclick={backdropClick}
	class="m-auto w-[min(26rem,calc(100vw-2rem))] rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
>
	<form onsubmit={submit} class="flex flex-col gap-4 p-6">
		<h2 class="text-[17px] font-bold">
			{record ? m.trip_total_title() : m.trip_finish_title()}
		</h2>

		{#if !record}
			<p class="text-sm text-tn-meta">{m.trip_finish_hint()}</p>
		{/if}

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.trip_total_label()}
			</span>

			<div
				class="flex h-12 items-center gap-2 rounded-lg border border-input bg-background px-3 focus-within:border-tn-primary"
			>
				<span class="flex-none font-bold text-tn-meta">€</span>

				<input
					bind:value={amount}
					inputmode="decimal"
					maxlength={12}
					placeholder={m.trip_total_placeholder()}
					class="h-full min-w-0 flex-1 bg-transparent font-semibold outline-none"
				/>
			</div>
		</label>

		{#if invalid}
			<p class="text-sm font-semibold text-destructive">{m.trip_total_invalid()}</p>
		{/if}

		{#if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex flex-wrap items-center gap-2">
			<!-- Suma je nepovinna: nakup sa da ukoncit bez nej a uz zapisanu sa da zase odobrat. -->
			{#if record}
				{#if record.totalCost != null}
					<button
						type="button"
						onclick={() => save(undefined)}
						class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted"
					>
						{m.trip_total_clear()}
					</button>
				{/if}
			{:else}
				<button
					type="button"
					onclick={() => save(undefined)}
					class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
				>
					{m.trip_finish_skip()}
				</button>
			{/if}

			<button
				type="button"
				onclick={() => (open = false)}
				class="ml-auto inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
			>
				{m.item_cancel()}
			</button>

			<button
				type="submit"
				disabled={pending || !amount.trim()}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
			>
				{#if pending}
					{record ? m.trip_total_pending() : m.shop_finish_pending()}
				{:else}
					{record ? m.trip_total_save() : m.shop_finish()}
				{/if}
			</button>
		</div>
	</form>
</dialog>
