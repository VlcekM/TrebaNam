<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { ApiError } from '$lib/api';
	import { CATEGORIES, categoryLabel } from '$lib/categories';
	import {
		createItem,
		deleteItem,
		fetchItemSuggestions,
		ItemNameMaxLength,
		ItemNoteMaxLength,
		ItemQuantityMaxLength,
		updateItem
	} from '$lib/items';
	import { m } from '$lib/paraglide/messages.js';
	import type { Item, ItemSuggestion } from '$lib/types';

	// Bez polozky sa zaklada nova, s polozkou sa upravuje - polia su v oboch pripadoch rovnake,
	// takze to je jeden dialog a nie dva takmer identicke.
	let { open = $bindable(false), item }: { open?: boolean; item?: Item } = $props();

	let dialog = $state<HTMLDialogElement>();
	let name = $state('');
	let quantity = $state('');
	let category = $state<string>('other');
	let note = $state('');
	let pending = $state(false);
	let failed = $state(false);
	// Duplicitu odmieta API, lebo len ono vidi cely zoznam; posle nazov tak, ako uz v zozname
	// je, takze clovek vidi aj ine pisanie toho isteho ("Banány" na napisane "banany").
	let duplicate = $state<string | undefined>();
	let suggestions = $state<ItemSuggestion[]>([]);

	/**
	 * Vacsina veci sa kupuje znova a znova, takze ich netreba pisat druhy raz. Prazdne pole
	 * ponuka najcastejsie, rozpisane ponuku zuzi - jedno pole je zoznam obluby aj naseptavac.
	 */
	const query = $derived(name.trim().toLowerCase());

	const matches = $derived(
		suggestions
			.filter((suggestion) => {
				const candidate = suggestion.name.toLowerCase();

				// Ked uz je napisane presne to, naseptavac nema co ponuknut.
				return !query || (candidate.includes(query) && candidate !== query);
			})
			.slice(0, 6)
	);

	// Nativny <dialog> uz vie modalitu, past na fokus aj zatvorenie Escapom, takze ho
	// len drzime v sulade so stavom stranky namiesto vlastnej implementacie toho isteho.
	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			name = item?.name ?? '';
			quantity = item?.quantity ?? '';
			category = item?.category ?? 'other';
			note = item?.note ?? '';
			failed = false;
			duplicate = undefined;
			// Pri uprave sa meni konkretna polozka, tak tam navrhy nedavaju zmysel.
			suggestions = [];
			if (!item) loadSuggestions();
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function loadSuggestions() {
		try {
			suggestions = await fetchItemSuggestions();
		} catch {
			// Naseptavac je pomocka, nie podmienka - bez neho sa polozka prida rovnako.
			suggestions = [];
		}
	}

	function pick(suggestion: ItemSuggestion) {
		name = suggestion.name;
		quantity = suggestion.quantity ?? '';
		category = suggestion.category;
	}

	async function run(action: () => Promise<unknown>) {
		if (pending) return;

		pending = true;
		failed = false;
		duplicate = undefined;

		try {
			await action();
			// Zoznam nesie load stranky, takze po zapise ho necham nacitat znova.
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

	// Kliknutie mimo formulara trafi samotny <dialog> - formular vyplna cely jeho vnutrajsok,
	// takze taky zasah moze prist len z tmaveho pozadia. Sledujeme aj stlacenie, inak by
	// oznacovanie textu tahom von z inputu dialog zavrelo.
	let pressedBackdrop = false;

	function backdropDown(event: MouseEvent) {
		pressedBackdrop = event.target === dialog;
	}

	function backdropClick(event: MouseEvent) {
		if (pressedBackdrop && event.target === dialog && !pending) {
			open = false;
		}
	}

	function submit(event: SubmitEvent) {
		event.preventDefault();

		if (!name.trim()) return;

		const fields = {
			name: name.trim(),
			quantity: quantity.trim() || undefined,
			category,
			note: note.trim() || undefined
		};

		run(() => (item ? updateItem(item.id, fields) : createItem(fields)));
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
		<h2 class="text-[17px] font-bold">{item ? m.item_edit() : m.item_add()}</h2>

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.item_name_label()}
			</span>
			<input
				bind:value={name}
				required
				maxlength={ItemNameMaxLength}
				placeholder={m.item_name_placeholder()}
				class="h-12 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
			/>
		</label>

		{#if matches.length}
			<div class="flex flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_suggest_heading()}
				</span>

				<div class="flex flex-wrap gap-2">
					{#each matches as suggestion (suggestion.name)}
						<button
							type="button"
							onclick={() => pick(suggestion)}
							title={suggestion.count === 1
								? m.item_suggest_count_one()
								: m.item_suggest_count({ count: suggestion.count })}
							class="inline-flex cursor-pointer items-center gap-1.5 rounded-lg bg-tn-tint px-3 py-2 text-[13px] font-bold text-tn-primary-strong transition hover:brightness-95"
						>
							{suggestion.name}

							{#if suggestion.quantity}
								<span class="font-semibold text-tn-meta">{suggestion.quantity}</span>
							{/if}
						</button>
					{/each}
				</div>
			</div>
		{/if}

		<div class="flex gap-3">
			<label class="flex flex-1 flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_quantity_label()}
				</span>
				<input
					bind:value={quantity}
					maxlength={ItemQuantityMaxLength}
					placeholder={m.item_quantity_placeholder()}
					class="h-12 w-full rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
				/>
			</label>

			<label class="flex flex-1 flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_category_label()}
				</span>
				<select
					bind:value={category}
					class="h-12 w-full cursor-pointer rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
				>
					{#each CATEGORIES as code (code)}
						<option value={code}>{categoryLabel(code)}</option>
					{/each}
				</select>
			</label>
		</div>

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.item_note_label()}
			</span>
			<textarea
				bind:value={note}
				rows="2"
				maxlength={ItemNoteMaxLength}
				placeholder={m.item_note_placeholder()}
				class="resize-none rounded-lg border border-input bg-background px-3 py-2.5 font-semibold outline-none focus-visible:border-tn-primary"
			></textarea>
		</label>

		{#if duplicate}
			<p class="text-sm font-semibold text-destructive">
				{m.item_duplicate({ name: duplicate })}
			</p>
		{:else if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex items-center gap-2">
			{#if item}
				<button
					type="button"
					onclick={() => run(() => deleteItem(item.id))}
					class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted"
				>
					{m.item_delete()}
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
				{#if pending}
					{item ? m.item_save_pending() : m.item_add_pending()}
				{:else}
					{item ? m.item_save() : m.item_add_submit()}
				{/if}
			</button>
		</div>
	</form>
</dialog>
