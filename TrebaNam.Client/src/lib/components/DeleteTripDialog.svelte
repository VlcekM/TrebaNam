<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { tripDate } from '$lib/dates';
	import { deleteTrip } from '$lib/shopping';
	import { m } from '$lib/paraglide/messages.js';
	import type { ShoppingRecord } from '$lib/types';

	// Zmazanie nakupu sa neda vratit, tak sa deje az po potvrdeni - tlacidlo je hned vedla
	// sumy a jeden nepresny prst by inak zmazal cely zaznam.
	let { open = $bindable(false), record }: { open?: boolean; record?: ShoppingRecord } = $props();

	let dialog = $state<HTMLDialogElement>();
	let pending = $state(false);
	let failed = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			failed = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function confirm() {
		if (pending || !record) return;

		pending = true;
		failed = false;

		try {
			await deleteTrip(record.id);
			await invalidateAll();
			open = false;
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
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
	<div class="flex flex-col gap-4 p-6">
		<h2 class="text-[17px] font-bold">{m.trip_delete_title()}</h2>

		{#if record}
			{@const date = tripDate(record.completedAt).long}

			<p class="text-sm text-tn-meta">
				{record.items.length === 1
					? m.trip_delete_body_one({ date })
					: m.trip_delete_body({ date, count: record.items.length })}
			</p>
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
				type="button"
				onclick={confirm}
				disabled={pending}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-destructive px-5 font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
			>
				{pending ? m.trip_delete_pending() : m.trip_delete_confirm()}
			</button>
		</div>
	</div>
</dialog>
