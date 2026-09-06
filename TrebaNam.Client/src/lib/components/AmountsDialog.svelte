<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { ItemQuantityMaxLength, setItemBoughtQuantity, updateItem } from '$lib/items';
	import { m } from '$lib/paraglide/messages.js';
	import type { Item } from '$lib/types';

	// Obe mnozstva naraz: kolko treba a kolko sa uz odnieslo. V obchode sa oboje meni z toho
	// isteho dovodu - v regali bolo nieco ine, nez sa cakalo - takze to je jeden dialog.
	let { open = $bindable(false), item }: { open?: boolean; item?: Item } = $props();

	let dialog = $state<HTMLDialogElement>();
	let needed = $state('');
	let got = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			needed = item?.quantity ?? '';
			got = item?.boughtQuantity ?? '';
			failed = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function save() {
		if (pending || !item) return;

		pending = true;
		failed = false;

		const wanted = needed.trim() || undefined;
		const bought = got.trim() || undefined;

		try {
			// Kolko treba je vlastnost polozky, kolko sa odnieslo je stav nakupu - su to dva
			// zapisy a posielame len ten, ktory sa naozaj zmenil.
			if (wanted !== (item.quantity ?? undefined)) {
				await updateItem(item.id, {
					listID: item.listID,
					name: item.name,
					quantity: wanted,
					category: item.category,
					note: item.note
				});
			}

			if (bought !== (item.boughtQuantity ?? undefined)) {
				await setItemBoughtQuantity(item.id, bought);
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
		save();
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
	class="m-auto max-h-[calc(100svh-2rem)] w-[min(26rem,calc(100vw-2rem))] overflow-y-auto rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
>
	<form onsubmit={submit} class="flex flex-col gap-4 p-6">
		<h2 class="text-[17px] font-bold">{m.shop_amounts_title()}</h2>

		{#if item}
			<p class="text-sm text-tn-meta">{m.shop_amounts_hint({ name: item.name })}</p>

			<div class="flex gap-3">
				<!-- Kolko treba: v obchode sa to meni casto, lebo balenia byvaju ine, nez sa cakalo. -->
				<label class="flex flex-1 flex-col gap-2">
					<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{m.shop_amount_needed()}
					</span>
					<input
						bind:value={needed}
						maxlength={ItemQuantityMaxLength}
						placeholder={m.item_quantity_placeholder()}
						class="h-12 w-full rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
					/>
				</label>

				<!-- Prazdne pole znamena, ze z polozky nie je v kosiku nic. -->
				<label class="flex flex-1 flex-col gap-2">
					<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{m.shop_partial_label()}
					</span>
					<input
						bind:value={got}
						maxlength={ItemQuantityMaxLength}
						placeholder={m.shop_partial_placeholder()}
						class="h-12 w-full rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
					/>
				</label>
			</div>
		{/if}

		{#if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex items-center gap-2">
			<button
				type="button"
				onclick={() => (open = false)}
				class="ml-auto inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
			>
				{m.item_cancel()}
			</button>

			<button
				type="submit"
				disabled={pending}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
			>
				{pending ? m.shop_partial_pending() : m.shop_partial_save()}
			</button>
		</div>
	</form>
</dialog>
