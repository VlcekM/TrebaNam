<script lang="ts">
	import CameraOffIcon from '@lucide/svelte/icons/camera-off';
	import ImageIcon from '@lucide/svelte/icons/image';
	import { ApiError } from '$lib/api';
	import { m } from '$lib/paraglide/messages.js';
	import { lookupReceipt, parseReceiptCode, qrReader } from '$lib/receipts';
	import type { ReceiptPreview } from '$lib/types';

	// Precitanie blocku - kamerou, z fotky alebo opisanim kodu - a jeho nahlad z eKasy. Je
	// prvym krokom nacitania blocku do historie aj ukoncenia nakupu s blockom, takze je zvlast.
	// Kamera bezi, kym je komponent na obrazovke: zmizne s nim, aj ked sa dialog zavrie.
	let {
		title,
		hint,
		onfound,
		oncancel
	}: {
		title: string;
		hint: string;
		onfound: (preview: ReceiptPreview) => void;
		oncancel: () => void;
	} = $props();

	type Problem = 'not_receipt' | 'not_found' | 'unavailable' | 'offline' | 'failed';

	let video = $state<HTMLVideoElement>();
	let photo = $state<HTMLInputElement>();
	let code = $state('');
	let looking = $state(false);
	let problem = $state<Problem | undefined>();
	let camera = $state<'starting' | 'on' | 'off'>('off');
	let lastScanned: string | undefined;

	$effect(() => {
		if (!video) return;

		const target = video;
		let stream: MediaStream | undefined;
		let timer: ReturnType<typeof setTimeout> | undefined;
		let stopped = false;

		async function start() {
			if (!navigator.mediaDevices?.getUserMedia) {
				camera = 'off';
				return;
			}

			camera = 'starting';

			try {
				stream = await navigator.mediaDevices.getUserMedia({
					video: { facingMode: 'environment' },
					audio: false
				});
			} catch {
				// Zakazana alebo ziadna kamera - ostava fotka a opisanie kodu.
				camera = 'off';
				return;
			}

			if (stopped) {
				stream.getTracks().forEach((track) => track.stop());
				return;
			}

			target.srcObject = stream;
			await target.play().catch(() => {});
			camera = 'on';

			const read = await qrReader();

			async function tick() {
				if (stopped) return;

				if (!looking && target.readyState >= 2) {
					const text = await read(target).catch(() => undefined);

					// Kod, ktory uz neprisiel (blocek este nie je v eKase), ostava v zabere dalej;
					// znova sa pyta az na iny kod. Opisany kod a fotka mozu skusit ten isty zamerne.
					if (text && text !== lastScanned && !stopped) {
						lastScanned = text;
						await found(text);
					}
				}

				timer = setTimeout(tick, 250);
			}

			tick();
		}

		start();

		// Pri odchode sa kazda stopa zastavi, inak by na telefone svietila kontrolka kamery.
		return () => {
			stopped = true;
			clearTimeout(timer);
			stream?.getTracks().forEach((track) => track.stop());
			target.srcObject = null;
		};
	});

	/** Text z QR kodu alebo z pola - z neho identifikator a z neho nahlad. */
	async function found(text: string) {
		const receiptID = parseReceiptCode(text);

		if (!receiptID) {
			problem = 'not_receipt';
			return;
		}

		if (looking) return;

		looking = true;
		problem = undefined;

		try {
			onfound(await lookupReceipt(receiptID));
		} catch (error) {
			if (error instanceof ApiError) {
				problem =
					error.status === 404 ? 'not_found' : error.status === 502 ? 'unavailable' : 'failed';
			} else {
				problem = 'offline';
			}
		} finally {
			looking = false;
		}
	}

	function submitCode(event: SubmitEvent) {
		event.preventDefault();

		if (code.trim()) found(code);
	}

	async function choosePhoto(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];

		if (!file) return;

		problem = undefined;

		try {
			const image = await createImageBitmap(file);
			const text = await (await qrReader())(image);

			image.close();

			if (text) await found(text);
			else problem = 'not_receipt';
		} catch {
			problem = 'not_receipt';
		} finally {
			if (photo) photo.value = '';
		}
	}

	const problemText = $derived(
		problem === 'not_receipt'
			? m.receipt_not_receipt()
			: problem === 'not_found'
				? m.receipt_not_found()
				: problem === 'unavailable'
					? m.receipt_unavailable()
					: problem === 'offline'
						? m.receipt_offline()
						: problem === 'failed'
							? m.error_generic()
							: undefined
	);
</script>

<!-- Na telefone cez celu vysku dialogu: kamera dostane vsetko, co ostane po poliach. -->
<div
	class="flex h-full flex-col gap-4 px-4 pt-[max(1rem,env(safe-area-inset-top))] pb-[max(1rem,env(safe-area-inset-bottom))] sm:h-auto sm:p-6"
>
	<div class="flex flex-col gap-1">
		<h2 class="text-[17px] font-bold">{title}</h2>
		<p class="text-sm text-tn-meta">{hint}</p>
	</div>

	<div
		class="relative flex min-h-48 w-full flex-1 items-center justify-center overflow-hidden rounded-xl bg-muted sm:aspect-[4/3] sm:flex-none"
	>
		<video
			bind:this={video}
			muted
			playsinline
			class="absolute inset-0 size-full object-cover {camera === 'on' ? '' : 'invisible'}"
		></video>

		{#if camera === 'on'}
			<!-- Ramik, kam namierit; kod sa precita aj mimo neho, je to len pre oko. -->
			<div
				class="pointer-events-none absolute inset-[18%] rounded-2xl border-2 border-white/80 shadow-[0_0_0_9999px_rgb(0_0_0/0.25)]"
			></div>
		{:else if camera === 'off'}
			<div class="flex flex-col items-center gap-2 px-6 text-center text-sm text-tn-meta">
				<CameraOffIcon class="size-6" />
				{m.receipt_no_camera()}
			</div>
		{/if}

		{#if looking}
			<div
				class="absolute inset-x-0 bottom-0 bg-black/60 px-4 py-2 text-center text-sm font-semibold text-white"
			>
				{m.receipt_looking()}
			</div>
		{/if}
	</div>

	<form onsubmit={submitCode} class="flex flex-col gap-2">
		<span class="text-[11px] font-bold tracking-[0.08em] text-tn-meta uppercase">
			{m.receipt_code_label()}
		</span>
		<div class="flex gap-2">
			<input
				bind:value={code}
				autocapitalize="characters"
				autocomplete="off"
				spellcheck="false"
				placeholder="O-…"
				class="h-12 min-w-0 flex-1 rounded-lg border border-input bg-background px-3 font-semibold outline-none focus-visible:border-tn-primary"
			/>
			<button
				type="submit"
				disabled={looking || !code.trim()}
				class="inline-flex h-12 flex-none cursor-pointer items-center rounded-lg bg-tn-primary px-4 font-bold text-primary-foreground transition hover:bg-tn-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
			>
				{m.receipt_look_up()}
			</button>
		</div>
	</form>

	{#if problemText}
		<p class="text-sm font-semibold text-destructive">{problemText}</p>
	{/if}

	<div class="flex flex-wrap items-center gap-2">
		<input bind:this={photo} type="file" accept="image/*" class="hidden" onchange={choosePhoto} />
		<button
			type="button"
			onclick={() => photo?.click()}
			class="inline-flex h-11 cursor-pointer items-center gap-2 rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
		>
			<ImageIcon class="size-4" />
			{m.receipt_from_photo()}
		</button>

		<button
			type="button"
			onclick={oncancel}
			class="ml-auto inline-flex h-11 cursor-pointer items-center rounded-lg px-4 font-bold text-tn-meta transition hover:bg-muted"
		>
			{m.item_cancel()}
		</button>
	</div>
</div>
