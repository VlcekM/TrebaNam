<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { ApiError } from '$lib/api';
	import { categoryName } from '$lib/categories';
	import { createCategory, deleteCategory, renameCategory } from '$lib/households';
	import { m } from '$lib/paraglide/messages.js';
	import type { Category } from '$lib/types';

	// Bez skupiny sa zaklada nova, so skupinou sa premenuva alebo rusi ta otvorena. Pole je
	// v oboch pripadoch to iste, takze je to jeden dialog a nie dva takmer rovnake.
	let { open = $bindable(false), category }: { open?: boolean; category?: Category } = $props();

	/** "Ostatne" je spodok, na ktory padne vsetko bez skupiny - zrusit sa neda. */
	const removable = $derived(category !== undefined && category.code !== 'other');

	let dialog = $state<HTMLDialogElement>();
	let name = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let duplicate = $state<string | undefined>();
	// Polozky sa nemazu, ale prejdu inam - to je zmena zoznamu, tak sa na nu pytame.
	let confirming = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			name = category ? categoryName(category) : '';
			failed = false;
			duplicate = undefined;
			confirming = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function run(action: () => Promise<unknown>) {
		if (pending) return;

		pending = true;
		failed = false;
		duplicate = undefined;

		try {
			await action();
			await invalidateAll();
			open = false;
		} catch (error) {
			duplicate =
				error instanceof ApiError && error.status === 409
					? (error.json<{ name?: string }>()?.name ?? name.trim())
					: undefined;

			failed = duplicate === undefined;
		} finally {
			pending = false;
		}
	}

	function submit(event: SubmitEvent) {
		event.preventDefault();

		if (!name.trim()) return;

		run(() => (category ? renameCategory(category.id, name.trim()) : createCategory(name.trim())));
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
		<h2 class="text-[17px] font-bold">
			{category ? m.category_edit() : m.category_new()}
		</h2>

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.category_name_label()}
			</span>
			<input
				bind:value={name}
				required
				maxlength={40}
				placeholder={m.category_name_placeholder()}
				class="h-12 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
			/>
		</label>

		{#if confirming}
			<p class="text-sm leading-relaxed text-tn-meta">{m.category_delete_body()}</p>
		{/if}

		{#if duplicate}
			<p class="text-sm font-semibold text-destructive">
				{m.category_duplicate({ name: duplicate })}
			</p>
		{:else if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex items-center gap-2">
			{#if removable && category}
				<button
					type="button"
					onclick={() =>
						confirming ? run(() => deleteCategory(category.id)) : (confirming = true)}
					disabled={pending}
					class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
				>
					{#if pending && confirming}
						{m.category_delete_pending()}
					{:else}
						{confirming ? m.category_delete_confirm() : m.category_delete()}
					{/if}
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
				disabled={pending || !name.trim()}
				class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
			>
				{#if pending && !confirming}
					{category ? m.item_save_pending() : m.category_create_pending()}
				{:else}
					{category ? m.item_save() : m.category_create()}
				{/if}
			</button>
		</div>
	</form>
</dialog>
