<script lang="ts">
	import PlusIcon from '@lucide/svelte/icons/plus';
	import ShoppingCartIcon from '@lucide/svelte/icons/shopping-cart';
	import { flip } from 'svelte/animate';
	import { fade, slide } from 'svelte/transition';
	import { byCategory, categoryLabel } from '$lib/categories';
	import ItemDialog from '$lib/components/ItemDialog.svelte';
	import MemberAvatar from '$lib/components/MemberAvatar.svelte';
	import { ms } from '$lib/motion';
	import { m } from '$lib/paraglide/messages.js';
	import type { Item } from '$lib/types';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Bez polozky sa v dialogu zaklada nova, s polozkou sa upravuje.
	let editing = $state<Item | undefined>();
	let dialogOpen = $state(false);

	const items = $derived(data.items);
	const groups = $derived(byCategory(items));
	const members = $derived(data.household?.members ?? []);

	// Kto polozku pridal, sa berie z domacnosti v layoute - poradie clena drzi jeho farbu.
	const adderOf = $derived((userID: string) => {
		const index = members.findIndex((member) => member.id === userID);

		return index < 0 ? undefined : { member: members[index], index };
	});

	function open(item?: Item) {
		editing = item;
		dialogOpen = true;
	}
</script>

<svelte:head>
	<title>{m.list_page_title()}</title>
</svelte:head>

<main
	class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16 lg:items-start lg:px-7 lg:py-6"
>
	<div class="flex w-full max-w-2xl flex-col gap-4 lg:max-w-3xl">
		<div class="flex items-start justify-between gap-3 px-1.5">
			<div class="flex min-w-0 flex-col gap-1">
				<h1 class="text-[30px] leading-tight font-bold tracking-tight lg:text-[24px]">
					{m.list_title()}
				</h1>
				<p class="text-sm text-tn-meta">
					{data.household?.name}{items.length
						? ` · ${items.length === 1 ? m.list_count_one() : m.list_count_other({ count: items.length })}`
						: ''}
				</p>
			</div>

			<!--
				Akcie stoja oproti nadpisu na kazdej sirke. Na uzkom displeji sa popisky skryvaju,
				lebo "Shop mode" aj "Add item" vedla 30px nadpisu by sa uz nezmestili - tlacidla
				ostavaju stvorcove 44px, popis nesie aria-label.
			-->
			<div class="flex flex-none items-center gap-2">
				{#if items.length}
					<a
						href="/app/shop"
						aria-label={m.shop_start()}
						class="inline-flex size-11 cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-tn-tint font-bold text-tn-primary-strong transition hover:brightness-95 sm:size-auto sm:h-11 sm:px-4 lg:h-[38px]"
					>
						<ShoppingCartIcon class="size-4 flex-none" />
						<span class="hidden sm:inline">{m.shop_start()}</span>
					</a>
				{/if}

				<button
					type="button"
					onclick={() => open()}
					aria-label={m.item_add()}
					class="inline-flex size-11 cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-tn-primary font-bold text-primary-foreground transition hover:bg-tn-primary-hover sm:size-auto sm:h-11 sm:px-4 lg:h-[38px]"
				>
					<PlusIcon class="size-4 flex-none" />
					<span class="hidden sm:inline">{m.item_add()}</span>
				</button>
			</div>
		</div>

		{#if groups.length}
			{#each groups as group (group.category)}
				<section class="flex flex-col gap-2" animate:flip={{ duration: ms(220) }}>
					<h2 class="px-1.5 text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
						{categoryLabel(group.category)}
					</h2>

					<ul class="flex flex-col rounded-2xl border border-tn-muted/40 bg-card px-4 shadow-xs">
						{#each group.items as item (item.id)}
							{@const adder = adderOf(item.addedByUserID)}

							<li
								class="border-t border-tn-muted/40 first:border-t-0"
								animate:flip={{ duration: ms(220) }}
								in:fade={{ duration: ms(180) }}
								out:slide={{ duration: ms(180) }}
							>
								<!-- Cely riadok otvara upravu; samostatna ceruzka by na mobile bola len dalsi maly ciel. -->
								<button
									type="button"
									onclick={() => open(item)}
									class="flex w-full cursor-pointer items-center gap-3 py-3.5 text-left"
								>
									<span class="flex min-w-0 flex-1 flex-col gap-0.5">
										<span class="font-bold">{item.name}</span>

										<!-- Poznamka patri k veci, nie k riadku vedla nej - preto pod nazvom. -->
										{#if item.note}
											<span class="text-[13px] leading-snug text-tn-meta">{item.note}</span>
										{/if}
									</span>

									{#if item.quantity}
										<span class="text-[13px] text-tn-faint">{item.quantity}</span>
									{/if}

									{#if adder}
										<span
											title={m.item_added_by({
												name: adder.member.givenName ?? adder.member.name ?? ''
											})}
										>
											<MemberAvatar
												member={adder.member}
												index={adder.index}
												class="size-6 text-[10px]"
											/>
										</span>
									{/if}
								</button>
							</li>
						{/each}
					</ul>
				</section>
			{/each}
		{:else}
			<section
				class="rounded-2xl border border-tn-muted/40 bg-card px-6 py-12 text-center shadow-xs"
			>
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.list_empty()}</p>
			</section>
		{/if}
	</div>
</main>

<ItemDialog bind:open={dialogOpen} item={editing} />
