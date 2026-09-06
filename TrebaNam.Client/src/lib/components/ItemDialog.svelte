<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import StarIcon from '@lucide/svelte/icons/star';
	import { ApiError } from '$lib/api';
	import { categoryName } from '$lib/categories';
	import {
		createItem,
		deleteItem,
		fetchItemSuggestions,
		ItemNameMaxLength,
		ItemNoteMaxLength,
		ItemQuantityMaxLength,
		setItemFavourite,
		updateItem
	} from '$lib/items';
	import { m } from '$lib/paraglide/messages.js';
	import type { Category, Item, ItemSuggestion, ShoppingList } from '$lib/types';

	// Bez polozky sa zaklada nova, s polozkou sa upravuje - polia su v oboch pripadoch rovnake,
	// takze to je jeden dialog a nie dva takmer identicke. Zoznam je ten otvoreny; pri uprave
	// rozhoduje ten, na ktorom polozka stoji.
	let {
		open = $bindable(false),
		item,
		list,
		lists = [],
		categories = []
	}: {
		open?: boolean;
		item?: Item;
		list?: ShoppingList;
		lists?: ShoppingList[];
		categories?: Category[];
	} = $props();

	let dialog = $state<HTMLDialogElement>();
	let listID = $state('');
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
		suggestions.filter((suggestion) => {
			const candidate = suggestion.name.toLowerCase();

			// Ked uz je napisane presne to, naseptavac nema co ponuknut.
			return !query || (candidate.includes(query) && candidate !== query);
		})
	);

	// Niektore veci sa kupuju kazdy tyzden a napriek tomu ich pocet nakupov nedostane hore -
	// hviezdicka je rozhodnutie domacnosti a to je viac nez statistika, tak stoji nad nou.
	const usuals = $derived(matches.filter((suggestion) => suggestion.isFavourite).slice(0, 4));

	const bought = $derived(matches.filter((suggestion) => !suggestion.isFavourite).slice(0, 6));

	// Nativny <dialog> uz vie modalitu, past na fokus aj zatvorenie Escapom, takze ho
	// len drzime v sulade so stavom stranky namiesto vlastnej implementacie toho isteho.
	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			listID = item?.listID ?? list?.id ?? lists[0]?.id ?? '';
			name = item?.name ?? '';
			quantity = item?.quantity ?? '';
			category = item?.category ?? 'other';
			note = item?.note ?? '';
			failed = false;
			duplicate = undefined;
			// Pri uprave sa meni konkretna polozka, tak tam navrhy nedavaju zmysel.
			suggestions = [];
			if (!item) loadSuggestions(listID);
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	// Navrhy sa tahaju pre ten zoznam, na ktory sa prave pridava - vynechavaju to, co uz na nom
	// stoji. Prehodenie zoznamu v poli nizsie ich uz neprenacita; duplicitu aj tak chyti API.
	async function loadSuggestions(forList: string) {
		try {
			suggestions = await fetchItemSuggestions(forList);
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

	/**
	 * Hviezdicka je drobnost pri navrhu, nie ulozenie formulara - prekreslime ju hned a zapis
	 * posleme popri tom. Ked neprejde, vrati sa tam, kde bola.
	 */
	async function star(suggestion: ItemSuggestion) {
		const next = !suggestion.isFavourite;

		suggestion.isFavourite = next;

		try {
			await setItemFavourite({
				name: suggestion.name,
				quantity: suggestion.quantity,
				category: suggestion.category,
				isFavourite: next
			});
		} catch {
			suggestion.isFavourite = !next;
		}
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
			listID,
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
	class="m-auto max-h-[calc(100svh-2rem)] w-[min(26rem,calc(100vw-2rem))] overflow-y-auto rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
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

		{#snippet chips(list: ItemSuggestion[])}
			<div class="flex flex-wrap gap-2">
				{#each list as suggestion (suggestion.name)}
					<!-- Navrh a hviezdicka su dve akcie, takze dve tlacidla vedla seba v jednom kruzku. -->
					<div
						class="inline-flex items-center rounded-lg bg-tn-tint text-[13px] font-bold text-tn-primary-strong"
					>
						<button
							type="button"
							onclick={() => pick(suggestion)}
							title={suggestion.count === 1
								? m.item_suggest_count_one()
								: m.item_suggest_count({ count: suggestion.count })}
							class="inline-flex cursor-pointer items-center gap-1.5 rounded-l-lg py-2 pr-1.5 pl-3 transition hover:brightness-95"
						>
							{suggestion.name}

							{#if suggestion.quantity}
								<span class="font-semibold text-tn-meta">{suggestion.quantity}</span>
							{/if}
						</button>

						<button
							type="button"
							onclick={() => star(suggestion)}
							aria-pressed={suggestion.isFavourite}
							aria-label={suggestion.isFavourite ? m.item_usual_remove() : m.item_usual_add()}
							class="inline-flex cursor-pointer items-center rounded-r-lg py-2 pr-2.5 pl-1 transition hover:brightness-95"
						>
							<StarIcon class="size-3.5 {suggestion.isFavourite ? 'fill-current' : 'opacity-40'}" />
						</button>
					</div>
				{/each}
			</div>
		{/snippet}

		{#if usuals.length}
			<div class="flex flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_usuals_heading()}
				</span>

				{@render chips(usuals)}
			</div>
		{/if}

		{#if bought.length}
			<div class="flex flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_suggest_heading()}
				</span>

				{@render chips(bought)}
			</div>
		{/if}

		<!--
			Kym je zoznam jediny, nie je sa kam rozhodovat a pole by len zavadzalo. Pri uprave je
			to zaroven presun: ze sa vec kupi az na chate, sa zisti az potom, co ju niekto napisal.
		-->
		{#if lists.length > 1}
			<label class="flex flex-col gap-2">
				<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{m.item_list_label()}
				</span>
				<select
					bind:value={listID}
					class="h-12 w-full cursor-pointer rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
				>
					{#each lists as one (one.id)}
						<option value={one.id}>{one.name}</option>
					{/each}
				</select>
			</label>
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
					{#each categories as one (one.id)}
						<option value={one.code}>{categoryName(one)}</option>
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
