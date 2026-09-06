<script lang="ts">
	import { m } from '$lib/paraglide/messages.js';

	// Potvrdenie pre veci, ktore sa nedaju vratit. Text aj samotnu akciu dodava volajuci -
	// dialog drzi len to, co maju vsetky spolocne: pozadie, cakanie a chybu.
	let {
		open = $bindable(false),
		title,
		body,
		confirmLabel,
		pendingLabel,
		confirm
	}: {
		open?: boolean;
		title: string;
		body: string;
		confirmLabel: string;
		pendingLabel: string;
		confirm: () => Promise<unknown>;
	} = $props();

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

	async function run() {
		if (pending) return;

		pending = true;
		failed = false;

		try {
			await confirm();
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
	class="m-auto max-h-[calc(100svh-2rem)] w-[min(26rem,calc(100vw-2rem))] overflow-y-auto rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
>
	<div class="flex flex-col gap-4 p-6">
		<h2 class="text-[17px] font-bold">{title}</h2>

		<p class="text-sm leading-relaxed text-tn-meta">{body}</p>

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
				onclick={run}
				disabled={pending}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-destructive px-5 font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
			>
				{pending ? pendingLabel : confirmLabel}
			</button>
		</div>
	</div>
</dialog>
