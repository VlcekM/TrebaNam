<script lang="ts">
	import type { HouseholdMember } from '$lib/types';
	import { cn } from '$lib/utils';

	// Handoff pocita s dvoma farbami clenov; poradie v domacnosti urcuje, kto ma ktoru.
	const COLORS = ['bg-tn-member-a', 'bg-tn-member-b'];

	let {
		member,
		index = 0,
		class: className
	}: { member: HouseholdMember; index?: number; class?: string } = $props();

	const initial = $derived((member.givenName ?? member.name ?? '?').slice(0, 1).toUpperCase());
</script>

{#if member.pictureUrl}
	<img
		src={member.pictureUrl}
		alt=""
		referrerpolicy="no-referrer"
		class={cn('size-11 flex-none rounded-full object-cover ring-2 ring-background', className)}
	/>
{:else}
	<span
		class={cn(
			'inline-flex size-11 flex-none items-center justify-center rounded-full font-bold text-white ring-2 ring-background',
			COLORS[index % COLORS.length],
			className
		)}
	>
		{initial}
	</span>
{/if}
