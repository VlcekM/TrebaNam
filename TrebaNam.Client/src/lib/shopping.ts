import { get } from 'svelte/store';
import { readSnapshot } from '$lib/offline/db';
import { cachedItems, newID, patchItems, patchRecords, write } from '$lib/offline/queue';
import { userStore } from '$lib/stores/user';
import type { ShoppingList, ShoppingRecord } from '$lib/types';

/**
 * Ukonci nakup jedneho zoznamu - odskrtnute polozky sa stanu zaznamom a zo zoznamu zmiznu.
 *
 * V obchode byva signal najhorsi prave pri pokladni, takze ukoncenie musi ist aj bez neho:
 * nakup dostane meno uz v telefone, historia si ho zapise hned a na server odide, ked sa da.
 * Rovnake meno znamena, ze ani z dvakrat odoslaneho ukoncenia nebudu dva nakupy.
 */
export function finishShopping(listID: string, totalCost?: number) {
	const id = newID();

	return write<ShoppingRecord>(
		{
			method: 'POST',
			path: '/api/shopping-records',
			body: { id, listID, totalCost: totalCost ?? null }
		},
		async () => {
			const items = await cachedItems();
			const lists = (await readSnapshot<ShoppingList[]>('/api/lists')) ?? [];
			const list = lists.find((one) => one.id === listID);

			// To iste, co robi server: cele odskrtnute aj ciastocne kupene, v poradi pridania.
			const bought = items.filter(
				(item) => item.listID === listID && (item.isChecked || item.boughtQuantity)
			);

			const record: ShoppingRecord = {
				id,
				completedByUserID: get(userStore)?.id ?? '',
				completedAt: new Date().toISOString(),
				listName: list?.name,
				listColor: list?.color,
				totalCost,
				items: bought.map((item) => ({
					id: newID(),
					name: item.name,
					quantity: item.boughtQuantity ?? item.quantity,
					category: item.category
				}))
			};

			await patchRecords((records) => [record, ...records]);

			// Cele polozky zo zoznamu odchadzaju, ciastocne v nom ostavaju - zvysok stale treba.
			await patchItems((all) =>
				all
					.filter((item) => !(item.listID === listID && item.isChecked))
					.map((item) =>
						item.listID === listID && item.boughtQuantity
							? { ...item, boughtQuantity: undefined }
							: item
					)
			);

			return record;
		}
	);
}

/** Zmaze ukonceny nakup aj s jeho riadkami; polozky sa do zoznamu nevracaju. */
export function deleteTrip(id: string) {
	return write<void>({ method: 'DELETE', path: `/api/shopping-records/${id}` }, async () => {
		await patchRecords((records) => records.filter((record) => record.id !== id));
	});
}

/** Doplni alebo opravi sumu za nakup; prazdna hodnota ju odoberie. */
export function setTripTotal(id: string, totalCost?: number) {
	return write<ShoppingRecord>(
		{
			method: 'PUT',
			path: `/api/shopping-records/${id}/total`,
			body: { totalCost: totalCost ?? null }
		},
		async () => {
			let changed: ShoppingRecord | undefined;

			await patchRecords((records) =>
				records.map((record) => {
					if (record.id !== id) return record;

					changed = { ...record, totalCost };

					return changed;
				})
			);

			return changed ?? ({ id } as ShoppingRecord);
		}
	);
}
