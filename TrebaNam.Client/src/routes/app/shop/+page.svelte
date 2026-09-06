<script lang="ts">
	import { flip } from 'svelte/animate';
	import { fade, slide } from 'svelte/transition';
	import { invalidateAll } from '$app/navigation';
	import CheckIcon from '@lucide/svelte/icons/check';
	import PercentIcon from '@lucide/svelte/icons/percent';
	import { byCategory, categoryLabel } from '$lib/categories';
	import PartialAmountDialog from '$lib/components/PartialAmountDialog.svelte';
	import TripTotalDialog from '$lib/components/TripTotalDialog.svelte';
	import { setItemChecked } from '$lib/items';
	import { ms } from '$lib/motion';
	import { m } from '$lib/paraglide/messages.js';
	import type { Item } from '$lib/types';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	/**
	 * V obchode sa odskrtava rychlo, takze riadok prepneme hned a poziadavku posleme popri tom;
	 * ked neprejde, prepis zmizne a riadok sa vrati tam, kde ho vidi zvysok domacnosti.
	 */
	let overrides = $state<Record<string, boolean>>({});
	let failed = $state(false);
	// Nakup sa ukoncuje v dialogu, lebo sa pri tom zapisuje aj to, kolko cely stal.
	let finishOpen = $state(false);

	// Ciastocne mnozstvo sa zadava v dialogu, nie klepnutim - je to text, nie prepinac.
	let partialOf = $state<Item | undefined>();
	let partialOpen = $state(false);

	const items = $derived(data.items);
	const isChecked = $derived((item: Item) => overrides[item.id] ?? item.isChecked);
	const inCart = $derived(items.filter(isChecked));
	const left = $derived(items.filter((item) => !isChecked(item)));
	const groups = $derived(byCategory(left));
	// Ciastocne kupene tiez patria do nakupu, takze ho maju cim ukoncit.
	const partial = $derived(left.filter((item) => item.boughtQuantity));

	async function toggle(item: Item) {
		const next = !isChecked(item);

		overrides[item.id] = next;
		failed = false;

		try {
			await setItemChecked(item.id, next);

			// Odskrtnutie ciastocny nakup na serveri zrusi - nech to sedi aj na obrazovke.
			if (item.boughtQuantity) await invalidateAll();
		} catch {
			delete overrides[item.id];
			failed = true;
		}
	}

	function askPartial(item: Item) {
		partialOf = item;
		partialOpen = true;
	}

	function finish() {
		if (inCart.length + partial.length === 0) return;

		finishOpen = true;
	}
</script>

<svelte:head>
	<title>{m.shop_page_title()}</title>
</svelte:head>

<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<!-- Hlavicka rezimu je podla handoffu akcentna karta, nie obycajny nadpis. -->
		<section class="flex flex-col gap-1 rounded-2xl bg-tn-tint p-5">
			<div class="flex items-center justify-between gap-3">
				<h1 class="text-[11px] font-bold tracking-[0.08em] text-tn-primary-strong uppercase">
					{m.shop_title()}
				</h1>

				<a href="/app/list" class="text-[13px] font-bold text-tn-primary-strong hover:underline">
					{m.shop_exit()}
				</a>
			</div>

			<p class="text-2xl font-bold">
				{m.shop_progress({ checked: inCart.length, count: items.length })}
			</p>

			<!-- Ciastocne kupene sa do "x z y" nezmestia, ale ukoncit nakup uz staci, tak o nich vieme. -->
			{#if partial.length}
				<p class="text-[13px] font-bold text-tn-primary-strong">
					{m.shop_partial_count({ count: partial.length })}
				</p>
			{/if}

			<div class="mt-1 h-1.5 overflow-hidden rounded-full bg-card">
				<div
					class="h-full rounded-full bg-tn-primary transition-all"
					style="width: {items.length ? (inCart.length / items.length) * 100 : 0}%"
				></div>
			</div>
		</section>

		{#if failed}
			<p class="px-1.5 text-sm font-semibold text-destructive">{m.error_generic()}</p>
		{/if}

		{#each groups as group (group.category)}
			<section class="flex flex-col gap-2" animate:flip={{ duration: ms(220) }}>
				<h2 class="px-1.5 text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
					{categoryLabel(group.category)}
				</h2>

				<ul class="flex flex-col rounded-2xl border border-tn-muted/40 bg-card px-4 shadow-xs">
					{#each group.items as item (item.id)}
						<li
							class="flex items-center gap-2 border-t border-tn-muted/40 first:border-t-0"
							animate:flip={{ duration: ms(220) }}
							in:fade={{ duration: ms(180) }}
							out:slide={{ duration: ms(180) }}
						>
							<!-- Kruzok berie celu polozku, tlacidlo vedla nastavuje odnesenu cast. -->
							<button
								type="button"
								onclick={() => toggle(item)}
								aria-pressed={isChecked(item)}
								class="flex min-w-0 flex-1 cursor-pointer items-center gap-3 py-4 text-left"
							>
								<span
									class="size-[26px] flex-none rounded-full border-2 border-tn-primary"
									aria-hidden="true"
								></span>

								<span class="flex min-w-0 flex-1 flex-col gap-0.5">
									<span class="text-[17px] font-bold">{item.name}</span>

									<!-- Poznamka je najviac potrebna prave v obchode, tak stoji pri nazve. -->
									{#if item.note}
										<span class="text-[13px] leading-snug text-tn-meta">{item.note}</span>
									{/if}
								</span>

								{#if item.quantity}
									<span class="flex-none text-sm text-tn-faint">{item.quantity}</span>
								{/if}
							</button>

							<button
								type="button"
								onclick={() => askPartial(item)}
								aria-label={m.shop_partial()}
								class="inline-flex h-11 flex-none cursor-pointer items-center justify-center gap-1.5 rounded-lg px-3 font-bold transition {item.boughtQuantity
									? 'bg-tn-tint text-[13px] text-tn-primary-strong'
									: 'text-tn-meta hover:bg-muted'}"
							>
								<PercentIcon class="size-4 flex-none" />

								{#if item.boughtQuantity}
									{m.shop_partial_got({ quantity: item.boughtQuantity })}
								{/if}
							</button>
						</li>
					{/each}
				</ul>
			</section>
		{/each}

		{#if inCart.length}
			<section class="flex flex-col gap-2">
				<h2 class="px-1.5 text-[11px] font-bold tracking-[0.08em] text-tn-primary-strong uppercase">
					{m.shop_in_cart()} · {inCart.length}
				</h2>

				<ul class="flex flex-col rounded-2xl bg-tn-tint px-4">
					{#each inCart as item (item.id)}
						<li
							class="border-t border-tn-muted/40 first:border-t-0"
							animate:flip={{ duration: ms(220) }}
							in:fade={{ duration: ms(180) }}
							out:slide={{ duration: ms(180) }}
						>
							<button
								type="button"
								onclick={() => toggle(item)}
								aria-pressed="true"
								class="flex w-full cursor-pointer items-center gap-3 py-4 text-left"
							>
								<span
									class="inline-flex size-[26px] flex-none items-center justify-center rounded-full bg-tn-primary text-primary-foreground"
									aria-hidden="true"
								>
									<CheckIcon class="size-4" />
								</span>

								<span class="min-w-0 flex-1 text-[17px] font-bold text-tn-faint line-through">
									{item.name}
								</span>
							</button>
						</li>
					{/each}
				</ul>
			</section>
		{/if}

		{#if items.length === 0}
			<section
				class="rounded-2xl border border-tn-muted/40 bg-card px-6 py-12 text-center shadow-xs"
			>
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.shop_empty()}</p>
			</section>
		{/if}

		<button
			type="button"
			onclick={finish}
			disabled={inCart.length + partial.length === 0}
			title={inCart.length + partial.length === 0 ? m.shop_nothing_checked() : undefined}
			class="inline-flex h-[50px] cursor-pointer items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
		>
			{m.shop_finish()}
		</button>
	</div>
</main>

<PartialAmountDialog bind:open={partialOpen} item={partialOf} />
<TripTotalDialog bind:open={finishOpen} />
