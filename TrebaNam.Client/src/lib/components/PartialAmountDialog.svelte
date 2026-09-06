<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { ItemQuantityMaxLength, setItemBoughtQuantity } from '$lib/items';
	import { m } from '$lib/paraglide/messages.js';
	import type { Item } from '$lib/types';

	// Kolko sa z polozky naozaj odnieslo. Vlastny dialog, nie uprava polozky - v obchode
	// sa nemeni to, co treba kupit, len to, co uz je v kosiku.
	let { open = $bindable(false), item }: { open?: boolean; item?: Item } = $props();

	let dialog = $state<HTMLDialogElement>();
	let quantity = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			quantity = item?.boughtQuantity ?? '';
			failed = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function save(value?: string) {
		if (pending || !item) return;

		pending = true;
		failed = false;

		try {
			await setItemBoughtQuantity(item.id, value);
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

		const value = quantity.trim();

		if (value) save(value);
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
		<h2 class="text-[17px] font-bold">{m.shop_partial_title()}</h2>

		{#if item}
			<p class="text-sm text-tn-meta">{m.shop_partial_hint({ name: item.name })}</p>

			<label class="flex flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.shop_partial_label()}
				</span>

				<div class="flex items-center gap-2">
					<input
						bind:value={quantity}
						required
						maxlength={ItemQuantityMaxLength}
						placeholder={m.shop_partial_placeholder()}
						class="h-12 min-w-0 flex-1 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
					/>

					<!-- Cele mnozstvo drzime na ociach, aby bolo z coho odhadovat tu cast. -->
					{#if item.quantity}
						<span class="flex-none text-sm text-tn-faint">
							{m.shop_partial_of({ quantity: item.quantity })}
						</span>
					{/if}
				</div>
			</label>
		{/if}

		{#if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex items-center gap-2">
			<!-- Zrusit ciastocny nakup ma zmysel len vtedy, ked uz nejaky je. -->
			{#if item?.boughtQuantity}
				<button
					type="button"
					onclick={() => save(undefined)}
					class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted"
				>
					{m.shop_partial_clear()}
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
				disabled={pending || !quantity.trim()}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
			>
				{pending ? m.shop_partial_pending() : m.shop_partial_save()}
			</button>
		</div>
	</form>
</dialog>
