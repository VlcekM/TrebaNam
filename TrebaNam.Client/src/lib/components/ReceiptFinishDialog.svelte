<script lang="ts">
	import { goto, invalidateAll } from '$app/navigation';
	import SparklesIcon from '@lucide/svelte/icons/sparkles';
	import { ApiError } from '$lib/api';
	import ReceiptScanner from '$lib/components/ReceiptScanner.svelte';
	import { tripDate } from '$lib/dates';
	import { money } from '$lib/money';
	import { m } from '$lib/paraglide/messages.js';
	import { nameKey, pairWithAI, receiptQuantity } from '$lib/receipts';
	import { finishShopping } from '$lib/shopping';
	import type { Item, ReceiptPreview, ShoppingRecord } from '$lib/types';

	// Ukoncenie nakupu s blockom: najprv sa blocek precita, potom sa riadky blocku sparuju
	// s tym, co je odskrtnute. Par si domacnost zapamata, takze nabuduce sa sparuje sam.
	// Co z blocku na zozname nebolo, ide do nakupu tak, ako je - kupilo sa to.
	let {
		open = $bindable(false),
		listID,
		bought = [],
		extraItemIDs = []
	}: {
		open?: boolean;
		listID: string;
		/** Co ide do nakupu: odskrtnute aj ciastocne kupene, z tohto zoznamu aj prinesene z inych. */
		bought?: Item[];
		extraItemIDs?: string[];
	} = $props();

	interface Line {
		receiptName: string;
		name: string;
		quantity: number;
		price: number;
		category: string;
		isDeposit: boolean;
		/** Sparovana vec z nakupu; prazdne, ked na zozname nebola. */
		itemID: string;
		/** Odkial par prisiel - aby clovek videl, co navrhla appka a co model. */
		source?: 'remembered' | 'ai';
		/** Nesparovany riadok ide do nakupu ako vec navyse, ked je zaskrtnuty. */
		include: boolean;
	}

	let dialog = $state<HTMLDialogElement>();
	let preview = $state<ReceiptPreview | undefined>();
	let lines = $state<Line[]>([]);
	let storeName = $state('');
	let pending = $state(false);
	let failed = $state(false);
	let already = $state<string | undefined>();
	let asking = $state(false);
	let aiNote = $state<string | undefined>();
	let pressedBackdrop = false;

	const unpaired = $derived(lines.filter((line) => !line.itemID));
	// Odskrtnute veci, ku ktorym na blocku nic nie je - do nakupu idu aj tak, len to clovek vidi.
	const missing = $derived(bought.filter((item) => !lines.some((line) => line.itemID === item.id)));

	$effect(() => {
		if (!dialog) return;

		if (open && !dialog.open) {
			preview = undefined;
			lines = [];
			failed = false;
			already = undefined;
			aiNote = undefined;
			dialog.showModal();
		} else if (!open && dialog.open) {
			dialog.close();
		}
	});

	/**
	 * Riadok sa sparuje sam, ked sa jeho nazov - zapamatany z minula alebo ten z blocku - zhoduje
	 * s nazvom odskrtnutej veci. Ostatne caka na cloveka alebo na model.
	 */
	function found(answer: ReceiptPreview) {
		preview = answer;
		already = answer.importedAt;
		storeName = answer.storeName ?? '';
		lines = answer.items.map((item) => {
			const match = bought.find((one) => nameKey(one.name) === nameKey(item.name));

			return {
				receiptName: item.receiptName,
				name: item.name,
				quantity: item.quantity,
				price: item.price,
				category: item.category,
				isDeposit: item.isDeposit,
				itemID: match?.id ?? '',
				// Odznak len tam, kde par naozaj prisiel z pamati, nie z nahodnej zhody nazvov.
				source: match && item.remembered ? 'remembered' : undefined,
				include: !item.isDeposit
			};
		});
	}

	/** Model dostane len to, co este sparovane nie je - co uz clovek alebo pamat rozhodli, plati. */
	async function askAI() {
		const waiting = lines.filter((line) => !line.itemID);

		if (asking || waiting.length === 0 || bought.length === 0) return;

		asking = true;
		aiNote = undefined;

		try {
			const pairs = await pairWithAI(
				waiting.map((line) => line.receiptName),
				bought.map((item) => item.name)
			);
			let paired = 0;

			for (const pair of pairs) {
				const line = waiting[pair.line];

				// Kym model rozmyslal, clovek mohol riadok sparovat sam - to ma prednost.
				if (!line || line.itemID) continue;

				line.itemID = bought[pair.item].id;
				line.source = 'ai';
				paired++;
			}

			aiNote = paired === 0 ? m.receipt_ai_nothing() : undefined;
		} catch (error) {
			aiNote =
				error instanceof ApiError
					? error.status === 503
						? m.receipt_ai_off()
						: m.receipt_ai_failed()
					: m.receipt_offline();
		} finally {
			asking = false;
		}
	}

	async function finish() {
		if (!preview || pending) return;

		pending = true;
		failed = false;

		try {
			await finishShopping(listID, preview.totalCost, extraItemIDs, {
				receiptID: preview.receiptID,
				storeName: storeName.trim() || undefined,
				extra: lines
					.filter((line) => !line.itemID && line.include)
					.map((line) => ({
						receiptName: line.receiptName,
						name: line.name,
						quantity: receiptQuantity(line.quantity) || undefined,
						category: line.category
					})),
				pairings: lines
					.filter((line) => line.itemID)
					.map((line) => ({ receiptName: line.receiptName, itemID: line.itemID }))
			});

			// Nakup skoncil a jeho polozky su uz len v historii - tam sa clovek aj pozrie.
			await goto('/app/history');
			await invalidateAll();
			open = false;
		} catch (error) {
			// 409 je aj "nic nie je odskrtnute" (zoznam medzitym ukoncil niekto iny) - blocek to nie je.
			const record =
				error instanceof ApiError && error.status === 409
					? error.json<ShoppingRecord>()
					: undefined;

			if (record?.id) {
				already = record.completedAt;
			} else {
				failed = true;
			}
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
	class="m-auto max-h-[calc(100dvh-2rem)] w-[min(36rem,calc(100vw-2rem))] rounded-2xl border border-tn-muted/40 bg-card p-0 text-foreground shadow-float"
>
	{#if !preview}
		{#if open}
			<ReceiptScanner
				title={m.receipt_finish_title()}
				hint={m.receipt_finish_hint()}
				onfound={found}
				oncancel={() => (open = false)}
			/>
		{/if}
	{:else}
		<div class="flex max-h-[calc(100dvh-2rem)] flex-col">
			<div class="flex flex-col gap-3 border-b border-tn-muted/40 p-6 pb-4">
				<div class="flex flex-wrap items-baseline gap-x-3 gap-y-1">
					<h2 class="text-[17px] font-bold">{m.receipt_pair_title()}</h2>
					<span class="text-[13px] text-tn-meta">
						{#if preview.totalCost != null}
							<span class="font-bold text-tn-primary-strong">{money(preview.totalCost)}</span>
						{/if}
					</span>
				</div>

				{#if already}
					<p class="rounded-lg bg-tn-tint px-3 py-2 text-sm font-semibold">
						{m.receipt_already({ date: tripDate(already).long })}
					</p>
				{/if}

				<label class="flex flex-col gap-2">
					<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{m.receipt_store_label()}
					</span>
					<input
						bind:value={storeName}
						maxlength={80}
						class="h-11 w-full rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
					/>
				</label>

				<p class="text-[13px] text-tn-meta">{m.receipt_pair_hint()}</p>

				{#if unpaired.length > 0 && bought.length > 0}
					<div class="flex flex-wrap items-center gap-x-3 gap-y-1">
						<button
							type="button"
							onclick={askAI}
							disabled={asking}
							class="inline-flex h-9 cursor-pointer items-center gap-2 rounded-lg bg-tn-tint px-3 text-[13.5px] font-bold text-tn-primary-strong transition hover:bg-muted disabled:cursor-wait disabled:opacity-60"
						>
							<SparklesIcon class="size-4" />
							{asking ? m.receipt_ai_pending() : m.receipt_ai_pair()}
						</button>
						{#if aiNote}
							<span class="text-[13px] text-tn-meta">{aiNote}</span>
						{/if}
					</div>
				{/if}
			</div>

			<ul class="flex min-h-0 flex-1 flex-col overflow-y-auto px-6">
				{#each lines as line, index (index)}
					<li
						class="flex flex-wrap items-center gap-x-3 gap-y-2 border-t border-tn-muted/40 py-3 first:border-t-0 {line.itemID ||
						line.include
							? ''
							: 'opacity-55'}"
					>
						<div class="flex min-w-0 flex-1 basis-44 flex-col">
							<span class="truncate font-bold">{line.receiptName}</span>
							<span class="text-[13px] text-tn-faint">
								{[receiptQuantity(line.quantity), money(line.price)].filter(Boolean).join(' · ')}
								{#if line.itemID && line.source === 'remembered'}
									· {m.receipt_pair_remembered()}
								{:else if line.itemID && line.source === 'ai'}
									· {m.receipt_pair_ai()}
								{/if}
							</span>
						</div>

						<div class="flex flex-1 basis-full items-center gap-2 sm:flex-none sm:basis-56">
							<select
								bind:value={line.itemID}
								onchange={() => (line.source = undefined)}
								aria-label={m.receipt_pair_with({ name: line.receiptName })}
								class="h-10 min-w-0 flex-1 cursor-pointer rounded-lg border bg-background px-2 text-sm font-semibold outline-none focus-visible:border-tn-primary {line.itemID
									? 'border-tn-primary/60'
									: 'border-input'}"
							>
								<option value="">{m.receipt_pair_none()}</option>
								{#each bought as item (item.id)}
									<option value={item.id}>{item.name}</option>
								{/each}
							</select>

							<!-- Co na zozname nebolo, sa kupilo aj tak - ide do nakupu, kym sa neodskrtne. -->
							{#if !line.itemID}
								<label
									class="flex flex-none cursor-pointer items-center gap-1.5 text-[13px] text-tn-meta"
								>
									<input
										type="checkbox"
										bind:checked={line.include}
										class="size-4 cursor-pointer accent-tn-primary"
									/>
									{m.receipt_pair_add()}
								</label>
							{/if}
						</div>
					</li>
				{/each}
			</ul>

			<div class="flex flex-col gap-3 border-t border-tn-muted/40 p-6 pt-4">
				{#if missing.length > 0}
					<p class="text-[13px] text-tn-meta">
						{m.receipt_pair_missing({ names: missing.map((item) => item.name).join(', ') })}
					</p>
				{/if}

				{#if failed}
					<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
				{/if}

				<div class="flex flex-wrap items-center gap-2">
					<button
						type="button"
						onclick={() => (open = false)}
						class="ml-auto inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
					>
						{m.item_cancel()}
					</button>

					<button
						type="button"
						onclick={finish}
						disabled={pending || !!already}
						class="inline-flex h-11 cursor-pointer items-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
					>
						{pending ? m.shop_finish_pending() : m.shop_finish()}
					</button>
				</div>
			</div>
		</div>
	{/if}
</dialog>
