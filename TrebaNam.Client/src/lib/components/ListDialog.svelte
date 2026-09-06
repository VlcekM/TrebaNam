<script lang="ts">
	import { goto, invalidateAll } from '$app/navigation';
	import CheckIcon from '@lucide/svelte/icons/check';
	import {
		createList,
		deleteList,
		LIST_COLORS,
		ListNameMaxLength,
		ListNoteMaxLength,
		listColorName,
		listDot,
		updateList
	} from '$lib/lists';
	import { m } from '$lib/paraglide/messages.js';
	import type { ShoppingList } from '$lib/types';

	// Bez zoznamu sa zaklada novy, so zoznamom sa upravuje ten otvoreny - polia su v oboch
	// pripadoch rovnake, takze to je jeden dialog a nie dva takmer identicke.
	let {
		open = $bindable(false),
		list,
		canDelete = false
	}: { open?: boolean; list?: ShoppingList; canDelete?: boolean } = $props();

	let dialog = $state<HTMLDialogElement>();
	let name = $state('');
	let color = $state<string>(LIST_COLORS[0]);
	let note = $state('');
	let pending = $state(false);
	let failed = $state(false);
	// Zmazanie berie so sebou aj to, co na zozname ostalo, takze sa pyta priamo tu.
	let confirming = $state(false);
	let pressedBackdrop = false;

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			name = list?.name ?? '';
			color = list?.color ?? LIST_COLORS[0];
			note = list?.note ?? '';
			failed = false;
			confirming = false;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	async function save(event: SubmitEvent) {
		event.preventDefault();

		if (pending || !name.trim()) return;

		pending = true;
		failed = false;

		const fields = {
			name: name.trim(),
			color,
			note: note.trim() || undefined
		};

		try {
			if (list) {
				await updateList(list.id, fields);
				await invalidateAll();
			} else {
				// Novy zoznam sa rovno otvori - zakladal sa preto, aby sa nan pridavalo.
				const created = await createList(fields);
				await goto(`/app/list/${created.id}`);
				await invalidateAll();
			}

			open = false;
		} catch {
			failed = true;
		} finally {
			pending = false;
		}
	}

	async function remove() {
		if (pending || !list) return;

		pending = true;
		failed = false;

		try {
			await deleteList(list.id);
			// Zoznam, na ktorom clovek stoji, prave zmizol - /app/list otvori ten prvy zvysny.
			await goto('/app/list');
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
	class="m-auto max-h-[calc(100svh-2rem)] w-[min(26rem,calc(100vw-2rem))] overflow-y-auto rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
>
	<form onsubmit={save} class="flex flex-col gap-4 p-6">
		<h2 class="text-[17px] font-bold">{list ? m.list_edit() : m.list_new()}</h2>

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.list_name_label()}
			</span>
			<input
				bind:value={name}
				required
				maxlength={ListNameMaxLength}
				placeholder={m.list_name_placeholder()}
				class="h-12 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
			/>
		</label>

		<!-- Farba je bodka pri nazve, takze sa aj vybera ako bodka a nie z rozbalovacieho pola. -->
		<div class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.list_color_label()}
			</span>

			<div class="flex flex-wrap gap-2">
				{#each LIST_COLORS as code (code)}
					<button
						type="button"
						onclick={() => (color = code)}
						aria-pressed={color === code}
						aria-label={listColorName(code)}
						class="inline-flex size-9 cursor-pointer items-center justify-center rounded-full text-white transition {listDot(
							code
						)} {color === code ? 'ring-2 ring-foreground ring-offset-2 ring-offset-card' : ''}"
					>
						{#if color === code}
							<CheckIcon class="size-4" />
						{/if}
					</button>
				{/each}
			</div>
		</div>

		<label class="flex flex-col gap-2">
			<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
				{m.list_note_label()}
			</span>
			<textarea
				bind:value={note}
				rows="2"
				maxlength={ListNoteMaxLength}
				placeholder={m.list_note_placeholder()}
				class="resize-none rounded-lg border border-input bg-background px-3 py-2.5 font-semibold outline-none focus-visible:border-tn-primary"
			></textarea>
		</label>

		{#if confirming}
			<p class="text-sm leading-relaxed text-tn-meta">
				{m.list_delete_body({ name: name.trim() })}
			</p>
		{/if}

		{#if failed}
			<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		<div class="flex items-center gap-2">
			<!-- Posledny zoznam sa zmazat neda: domacnost by nemala kam pridat prvu vec. -->
			{#if list && canDelete}
				<button
					type="button"
					onclick={() => (confirming ? remove() : (confirming = true))}
					disabled={pending}
					class="inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-destructive transition hover:bg-muted disabled:cursor-not-allowed disabled:opacity-50"
				>
					{#if pending && confirming}
						{m.list_delete_pending()}
					{:else}
						{confirming ? m.list_delete_confirm() : m.list_delete()}
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
					{list ? m.item_save_pending() : m.list_create_pending()}
				{:else}
					{list ? m.item_save() : m.list_create()}
				{/if}
			</button>
		</div>
	</form>
</dialog>
