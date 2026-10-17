<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { ApiError } from '$lib/api';
	import { categoryName } from '$lib/categories';
	import ReceiptScanner from '$lib/components/ReceiptScanner.svelte';
	import { tripDate } from '$lib/dates';
	import { ItemNameMaxLength, ItemQuantityMaxLength } from '$lib/items';
	import { money } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import { importReceipt, receiptQuantity } from '$lib/receipts';
	import type { Category, ReceiptPreview, ShoppingList, ShoppingRecord } from '$lib/types';

	// Dva kroky v jednom dialogu: najprv sa blocek precita, potom sa ukaze, co na nom je,
	// a clovek povie, co z toho do nakupu patri a ako sa to u nich vola.
	let {
		open = $bindable(false),
		lists = [],
		categories = []
	}: {
		open?: boolean;
		lists?: ShoppingList[];
		categories?: Category[];
	} = $props();

	interface Row {
		include: boolean;
		receiptName: string;
		name: string;
		quantity: string;
		category: string;
		price: number;
	}

	let dialog = $state<HTMLDialogElement>();
	let preview = $state<ReceiptPreview | undefined>();
	let rows = $state<Row[]>([]);
	let storeName = $state('');
	let listID = $state('');
	let importing = $state(false);
	let failed = $state(false);
	// Nakup z tohto blocku, ktory uz v historii je - z nahladu alebo z odmietnutia pri ulozeni.
	let already = $state<string | undefined>();
	let pressedBackdrop = false;

	const picked = $derived(rows.filter((row) => row.include && row.name.trim()));

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			reset();
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	function reset() {
		preview = undefined;
		rows = [];
		storeName = '';
		listID = lists[0]?.id ?? '';
		failed = false;
		already = undefined;
	}

	function found(answer: ReceiptPreview) {
		preview = answer;
		storeName = answer.storeName ?? '';
		already = answer.importedAt;
		rows = answer.items.map((item) => ({
			include: !item.isDeposit,
			receiptName: item.receiptName,
			name: item.name,
			quantity: receiptQuantity(item.quantity),
			category: item.category,
			price: item.price
		}));
	}

	async function save() {
		if (!preview || importing || picked.length === 0) return;

		importing = true;
		failed = false;

		try {
			await importReceipt(
				preview,
				picked.map((row) => ({
					receiptName: row.receiptName,
					name: row.name.trim(),
					quantity: row.quantity.trim() || undefined,
					category: row.category
				})),
				storeName.trim() || undefined,
				lists.find((one) => one.id === listID)
			);

			await invalidateAll();
			open = false;
		} catch (error) {
			if (error instanceof ApiError && error.status === 409) {
				already = error.json<ShoppingRecord>()?.completedAt ?? preview.issuedAt ?? '';
			} else {
				failed = true;
			}
		} finally {
			importing = false;
		}
	}

	function backdropDown(event: MouseEvent) {
		pressedBackdrop = event.target === dialog;
	}

	function backdropClick(event: MouseEvent) {
		if (pressedBackdrop && event.target === dialog && !importing) {
			open = false;
		}
	}
</script>

<dialog
	bind:this={dialog}
	onclose={() => (open = false)}
	onmousedown={backdropDown}
	onclick={backdropClick}
	class="m-0 h-dvh max-h-none w-screen max-w-none bg-card p-0 text-foreground sm:m-auto sm:h-auto sm:max-h-[calc(100dvh-2rem)] sm:w-[min(36rem,calc(100vw-2rem))] sm:rounded-2xl sm:border sm:border-tn-muted/40 sm:shadow-float"
>
	{#if !preview}
		{#if open}
			<ReceiptScanner
				title={m.receipt_title()}
				hint={m.receipt_hint()}
				onfound={found}
				oncancel={() => (open = false)}
			/>
		{/if}
	{:else}
		<div class="flex h-full flex-col sm:max-h-[calc(100dvh-2rem)]">
			<div
				class="flex flex-col gap-3 border-b border-tn-muted/40 px-4 pt-[max(1rem,env(safe-area-inset-top))] pb-3 sm:gap-4 sm:p-6 sm:pb-4"
			>
				<div class="flex flex-wrap items-baseline gap-x-3 gap-y-1">
					<h2 class="text-[17px] font-bold">{m.receipt_review_title()}</h2>
					<span class="text-[13px] text-tn-meta">
						{preview.issuedAt ? tripDate(preview.issuedAt).long : ''}
						{#if preview.totalCost != null}
							· <span class="font-bold text-tn-primary-strong">{money(preview.totalCost)}</span>
						{/if}
					</span>
				</div>

				{#if already}
					<p class="rounded-lg bg-tn-tint px-3 py-2 text-sm font-semibold">
						{m.receipt_already({ date: tripDate(already).long })}
					</p>
				{/if}

				<div class="flex flex-col gap-3 sm:flex-row">
					<label class="flex flex-1 flex-col gap-2">
						<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
							{m.receipt_store_label()}
						</span>
						<input
							bind:value={storeName}
							maxlength={80}
							class="h-12 w-full rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
						/>
					</label>

					<!-- Blocek o zoznamoch nic nevie; pri jednom zozname netreba vyberat. -->
					{#if lists.length > 1}
						<label class="flex flex-1 flex-col gap-2">
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
				</div>

				<p class="text-[13px] text-tn-meta">{m.receipt_review_hint()}</p>
			</div>

			<ul class="flex min-h-0 flex-1 flex-col overflow-y-auto overscroll-contain px-4 sm:px-6">
				{#each rows as row, index (index)}
					<li
						class="flex flex-wrap items-center gap-x-3 gap-y-2 border-t border-tn-muted/40 py-3 first:border-t-0 {row.include
							? ''
							: 'opacity-55'}"
					>
						<input
							type="checkbox"
							bind:checked={row.include}
							aria-label={m.receipt_include()}
							class="size-5 flex-none cursor-pointer accent-tn-primary"
						/>

						<input
							bind:value={row.name}
							maxlength={ItemNameMaxLength}
							aria-label={m.item_name_label()}
							class="h-10 min-w-0 flex-1 basis-40 rounded-lg border border-transparent bg-transparent px-2 font-bold outline-none hover:border-input focus-visible:border-tn-primary focus-visible:bg-background"
						/>

						<div
							class="ml-8 flex flex-1 basis-full items-center gap-2 sm:ml-0 sm:flex-none sm:basis-auto"
						>
							<input
								bind:value={row.quantity}
								maxlength={ItemQuantityMaxLength}
								aria-label={m.item_quantity_label()}
								placeholder="1"
								class="h-10 w-16 rounded-lg border border-input bg-background px-2 text-sm font-semibold outline-none focus-visible:border-tn-primary"
							/>

							<select
								bind:value={row.category}
								aria-label={m.item_category_label()}
								class="h-10 min-w-0 flex-1 cursor-pointer rounded-lg border border-input bg-background px-2 text-sm font-semibold outline-none focus-visible:border-tn-primary sm:w-32 sm:flex-none"
							>
								{#each categories as one (one.id)}
									<option value={one.code}>{categoryName(one)}</option>
								{/each}
							</select>

							<span class="w-16 flex-none text-right text-[13px] text-tn-faint">
								{money(row.price)}
							</span>
						</div>
					</li>
				{/each}
			</ul>

			<div
				class="flex flex-col gap-3 border-t border-tn-muted/40 px-4 pt-3 pb-[max(1rem,env(safe-area-inset-bottom))] sm:p-6 sm:pt-4"
			>
				{#if failed}
					<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
				{/if}

				<div class="flex items-center gap-2">
					<button
						type="button"
						onclick={() => (open = false)}
						class="inline-flex h-12 cursor-pointer items-center justify-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted sm:ml-auto sm:h-11"
					>
						{m.item_cancel()}
					</button>

					<button
						type="button"
						onclick={save}
						disabled={importing || picked.length === 0 || !!already}
						class="inline-flex h-12 flex-1 cursor-pointer items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50 sm:h-11 sm:flex-none"
					>
						{importing ? m.receipt_importing() : m.receipt_import()}
					</button>
				</div>
			</div>
		</div>
	{/if}
</dialog>
