<script lang="ts">
	import { goto } from '$app/navigation';
	import { joinHousehold } from '$lib/households';
	import { m } from '$lib/paraglide/messages.js';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let pending = $state(false);
	let failed = $state(false);

	const invite = $derived(data.invite);
	const blocked = $derived(invite?.alreadyMember || invite?.inAnotherHousehold);

	async function accept() {
		if (pending) return;

		pending = true;
		failed = false;

		try {
			await joinHousehold(data.code);
			// Plna navigacia s invalidateAll, aby /app nacital domacnost nanovo.
			await goto('/app', { invalidateAll: true });
		} catch {
			failed = true;
			pending = false;
		}
	}
</script>

<svelte:head>
	<title>{m.join_page_title()}</title>
</svelte:head>

<main class="flex flex-1 flex-col items-center justify-center px-4 py-12">
	<div
		class="flex w-full max-w-md flex-col gap-5 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs sm:p-8"
	>
		{#if !invite}
			<h1 class="text-[30px] leading-tight font-bold tracking-tight">{m.join_not_found()}</h1>
			<a
				href="/app"
				class="inline-flex h-[50px] items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover"
			>
				{m.join_open_app()}
			</a>
		{:else}
			<div class="flex flex-col gap-2">
				<!-- Ked sa pridat neda, netreba sa pytat - nadpis vtedy len pomenuje domacnost. -->
				<h1 class="text-[30px] leading-tight font-bold tracking-tight text-balance">
					{blocked ? invite.name : m.join_heading({ name: invite.name })}
				</h1>

				<p class="text-sm text-tn-meta">
					{invite.memberCount === 1
						? m.household_member_count_one()
						: m.household_member_count_other({ count: invite.memberCount })}
				</p>
			</div>

			{#if invite.alreadyMember}
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.join_already_member()}</p>
			{:else if invite.inAnotherHousehold}
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.join_in_another()}</p>
			{:else}
				<p class="text-[15px] leading-relaxed text-muted-foreground">{m.join_body()}</p>
			{/if}

			{#if failed}
				<p class="text-sm font-semibold text-destructive">{m.error_generic()}</p>
			{/if}

			{#if blocked}
				<a
					href="/app"
					class="inline-flex h-[50px] items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover"
				>
					{m.join_open_app()}
				</a>
			{:else}
				<button
					type="button"
					onclick={accept}
					disabled={pending}
					class="inline-flex h-[50px] cursor-pointer items-center justify-center rounded-lg bg-tn-primary px-5 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
				>
					{pending ? m.join_accept_pending() : m.join_accept()}
				</button>
			{/if}
		{/if}
	</div>
</main>
