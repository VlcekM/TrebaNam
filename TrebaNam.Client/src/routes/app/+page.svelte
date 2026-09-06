<script lang="ts">
	import { userStore } from '$lib/stores/user';
	import { m } from '$lib/paraglide/messages.js';
</script>

<svelte:head>
	<title>{m.app_page_title()}</title>
</svelte:head>

<main class="flex flex-1 flex-col items-center px-4 pt-6 pb-12 sm:pt-8 sm:pb-16">
	<div class="flex w-full max-w-2xl flex-col gap-4">
		<div class="flex flex-col gap-1 px-1.5">
			<h1 class="text-[30px] leading-tight font-bold tracking-tight">
				{#if $userStore?.givenName}
					{m.app_welcome({ name: $userStore.givenName })}
				{:else}
					{m.app_welcome_anonymous()}
				{/if}
			</h1>

			{#if $userStore?.emailAddress}
				<p class="text-sm text-tn-meta">
					{m.app_signed_in_as({ email: $userStore.emailAddress })}
				</p>
			{/if}
		</div>

		<!-- Karta ma tvar zoznamovej karty z handoffu: biela, 16px radius, jemny teply tien. -->
		<div class="flex flex-col gap-1.5 rounded-2xl border border-tn-muted/40 bg-card p-6 shadow-xs">
			<h2 class="text-[17px] font-bold">
				{m.app_empty_state_title()}
			</h2>

			<p class="text-sm leading-relaxed text-muted-foreground">
				{m.app_empty_state()}
			</p>
		</div>
	</div>
</main>
