import { get } from 'svelte/store';
import { readSnapshot } from '$lib/offline/db';
import {
	cachedItems,
	newID,
	patchItems,
	patchLists,
	patchRecords,
	write
} from '$lib/offline/queue';
import { userStore } from '$lib/stores/user';
import type { Item, ShoppingList, ShoppingRecord } from '$lib/types';

/**
 * Povie domacnosti, ze sa z tohto zoznamu prave nakupuje. Vola sa pri otvoreni rezimu nakupu
 * a bezici nakup nemeni - kto sa pripoji, pripoji sa k tomu, co uz zacal ten prvy. Ide cez rad
 * ako odskrtavanie: v obchode signal nebyva a nakup sa preto nema cim zdrzat.
 */
export function startShopping(listID: string) {
	const startedAt = new Date().toISOString();
	const startedBy = get(userStore)?.id ?? '';

	return write<ShoppingList>(
		{ method: 'POST', path: `/api/lists/${listID}/shopping` },
		async () => {
			let changed: ShoppingList | undefined;

			await patchLists((lists) =>
				lists.map((list) => {
					if (list.id !== listID) return list;

					changed = list.shoppingStartedAt
						? list
						: { ...list, shoppingStartedAt: startedAt, shoppingStartedByUserID: startedBy };

					return changed;
				})
			);

			return changed ?? ({ id: listID } as ShoppingList);
		}
	);
}

/** Zrusi bezici nakup bez ukoncenia; co je odskrtnute, ostava odskrtnute. */
export function cancelShopping(listID: string) {
	return write<ShoppingList>(
		{ method: 'DELETE', path: `/api/lists/${listID}/shopping` },
		async () => {
			let changed: ShoppingList | undefined;

			await patchLists((lists) =>
				lists.map((list) => {
					if (list.id !== listID) return list;

					changed = { ...list, shoppingStartedAt: undefined, shoppingStartedByUserID: undefined };

					return changed;
				})
			);

			return changed ?? ({ id: listID } as ShoppingList);
		}
	);
}

/** Zoznam, z ktoreho sa prave nakupuje - v poradi prepinaca, takze pri dvoch ten prvy. */
export function shoppingInProgress(lists: ShoppingList[]) {
	return lists.find((list) => list.shoppingStartedAt);
}

/**
 * Ukonci nakup jedneho zoznamu - odskrtnute polozky sa stanu zaznamom a zo zoznamu zmiznu.
 *
 * V obchode byva signal najhorsi prave pri pokladni, takze ukoncenie musi ist aj bez neho:
 * nakup dostane meno uz v telefone, historia si ho zapise hned a na server odide, ked sa da.
 * Rovnake meno znamena, ze ani z dvakrat odoslaneho ukoncenia nebudu dva nakupy.
 */
export function finishShopping(listID: string, totalCost?: number, extraItemIDs: string[] = []) {
	const id = newID();
	const extra = new Set(extraItemIDs);
	// Vec z tohto zoznamu alebo jedna z tych, ktore sa vzali z inych - to iste, co robi server.
	const taken = (item: Item) => item.listID === listID || extra.has(item.id);

	return write<ShoppingRecord>(
		{
			method: 'POST',
			path: '/api/shopping-records',
			body: { id, listID, totalCost: totalCost ?? null, extraItemIDs }
		},
		async () => {
			const items = await cachedItems();
			const lists = (await readSnapshot<ShoppingList[]>('/api/lists')) ?? [];
			const list = lists.find((one) => one.id === listID);

			// To iste, co robi server: cele odskrtnute aj ciastocne kupene, v poradi pridania.
			const bought = items.filter((item) => taken(item) && (item.isChecked || item.boughtQuantity));

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

			// Nakup skoncil - na prehlade uz nebezi, presne ako to spravi server.
			await patchLists((all) =>
				all.map((one) =>
					one.id === listID
						? { ...one, shoppingStartedAt: undefined, shoppingStartedByUserID: undefined }
						: one
				)
			);

			// Cele polozky zo zoznamu odchadzaju, ciastocne v nom ostavaju - zvysok stale treba.
			await patchItems((all) =>
				all
					.filter((item) => !(taken(item) && item.isChecked))
					.map((item) =>
						taken(item) && item.boughtQuantity ? { ...item, boughtQuantity: undefined } : item
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
