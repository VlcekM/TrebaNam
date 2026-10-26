import { get } from 'svelte/store';
import { api } from '$lib/api';
import { formatLocale } from '$lib/locale';
import { newID, patchRecords, write } from '$lib/offline/queue';
import { userStore } from '$lib/stores/user';
import type { ReceiptPreview, ShoppingList, ShoppingRecord } from '$lib/types';

/**
 * Identifikator blocku z textu QR kodu. Online blocek z eKasy ma v kode "O-" a 32 sestnastkovych
 * znakov; hladame ho kdekolvek v texte, takze projde aj kod, ktory ho bali do adresy. Offline
 * blocky (pokladna bez spojenia) taky identifikator nemaju - tie sa najst nedaju.
 */
export function parseReceiptCode(text: string): string | undefined {
	return text.match(/O-[0-9A-F]{32}/i)?.[0].toUpperCase();
}

/**
 * Nahlad blocku. Ide rovno na server, nikdy cez ulozene odpovede: stary nahlad by mohol
 * tvrdit, ze blocek este nacitany nie je, ked uz je. Bez signalu sa blocek jednoducho nenajde.
 */
export function lookupReceipt(receiptID: string) {
	return api<ReceiptPreview>(`/api/receipts/${encodeURIComponent(receiptID)}`);
}

/**
 * Kluc nazvu na porovnanie - bez diakritiky, velkych pismen a zdvojenych medzier, rovnako ako
 * ItemName.Key v API. "Banány" a "banany" su ta ista vec, aj ked jedna prisla z blocku.
 */
export function nameKey(name: string) {
	return name.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().replace(/\s+/g, ' ').trim();
}

/**
 * Mnozstvo z pokladne ako text, akym sa pise do zoznamu. Jeden kus netreba pisat vobec, kusy
 * su "4×" v oboch jazykoch a vazeny tovar ostava cislom, ako ho dala vaha - jednotku blocek
 * neuvadza a vymyslat ju nebudeme.
 */
export function receiptQuantity(quantity: number) {
	if (quantity === 1) return '';

	if (Number.isInteger(quantity)) return `${quantity}×`;

	return new Intl.NumberFormat(formatLocale(), { maximumFractionDigits: 3 }).format(quantity);
}

export interface ReceiptRow {
	/** Riadok blocku, z ktoreho vznikol; podla neho sa nabuduce ponukne to iste. */
	receiptName?: string;
	name: string;
	quantity?: string;
	category: string;
}

/**
 * Zapise blocek ako ukonceny nakup. Identifikator mu da telefon, rovnako ako pri ukonceni
 * nakupu, takze ani dvakrat odoslany zapis nespravi dva nakupy; ten isty blocek naskenovany
 * dvakrat server odmietne s nakupom, ktory uz ma.
 */
export function importReceipt(
	preview: ReceiptPreview,
	rows: ReceiptRow[],
	storeName: string | undefined,
	list: ShoppingList | undefined,
	totalCost: number | undefined
) {
	const id = newID();
	const completedAt = preview.issuedAt ?? new Date().toISOString();

	return write<ShoppingRecord>(
		{
			method: 'POST',
			path: `/api/receipts/${preview.receiptID}/import`,
			body: {
				id,
				listID: list?.id ?? null,
				storeName: storeName ?? null,
				completedAt,
				totalCost: totalCost ?? null,
				items: rows
			}
		},
		async () => {
			const record: ShoppingRecord = {
				id,
				completedByUserID: get(userStore)?.id ?? '',
				completedAt,
				listName: list?.name,
				listColor: list?.color,
				totalCost,
				receiptID: preview.receiptID,
				storeName,
				items: rows.map((row) => ({ id: newID(), ...row }))
			};

			// Historia je od najnovsieho a blocek moze byt starsi nez posledny nakup.
			await patchRecords((records) =>
				[record, ...records].sort((a, b) => b.completedAt.localeCompare(a.completedAt))
			);

			return record;
		}
	);
}

/**
 * Necha model navrhnut, ktory riadok blocku je ktora vec z nakupu. Vracia dvojice indexov do
 * poslanych poli. 503 znamena, ze na serveri nie je kluc k API Anthropicu, 502, ze model neodpovedal.
 */
export async function pairWithAI(lines: string[], items: string[]) {
	const answer = await api<{ pairs: { line: number; item: number }[] }>('/api/receipts/pair', {
		method: 'POST',
		body: JSON.stringify({ lines, items })
	});

	return answer.pairs;
}

/** To, co z BarcodeDetectora treba; TypeScript ho v DOM typoch este nema. */
interface Detector {
	detect(source: CanvasImageSource): Promise<{ rawValue: string }[]>;
}

declare global {
	interface Window {
		BarcodeDetector?: {
			new (options: { formats: string[] }): Detector;
			getSupportedFormats(): Promise<string[]>;
		};
	}
}

/**
 * Citac QR kodov. Kde prehliadac vie QR citat sam (Chrome na Androide), pouzije sa jeho - je
 * rychlejsi a zvladne aj pokrceny papier. Inde (Safari) sa dotiahne jsQR, az ked ho treba.
 */
export async function qrReader(): Promise<
	(source: HTMLVideoElement | HTMLCanvasElement | ImageBitmap) => Promise<string | undefined>
> {
	const native = window.BarcodeDetector;

	if (
		native &&
		(await native.getSupportedFormats().catch((): string[] => [])).includes('qr_code')
	) {
		const detector = new native({ formats: ['qr_code'] });

		return async (source) => (await detector.detect(source))[0]?.rawValue;
	}

	const { default: jsQR } = await import('jsqr');
	const canvas = document.createElement('canvas');
	const context = canvas.getContext('2d', { willReadFrequently: true })!;

	return async (source) => {
		const width = source instanceof HTMLVideoElement ? source.videoWidth : source.width;
		const height = source instanceof HTMLVideoElement ? source.videoHeight : source.height;

		if (!width || !height) return undefined;

		// Fotka z galerie ma aj 12 megapixelov; QR kod z blocku sa precita aj z mensej.
		const scale = Math.min(1, 1280 / Math.max(width, height));

		canvas.width = Math.round(width * scale);
		canvas.height = Math.round(height * scale);
		context.drawImage(source, 0, 0, canvas.width, canvas.height);

		const image = context.getImageData(0, 0, canvas.width, canvas.height);

		return jsQR(image.data, image.width, image.height)?.data;
	};
}
