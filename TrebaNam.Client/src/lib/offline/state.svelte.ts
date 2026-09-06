/**
 * Co appka prave vie o pripojeni a o zapisoch, ktore este neodisli. Je to jeden objekt, lebo
 * lista nad obrazovkou aj odosielanie citaju to iste - kto z nich to zmeni, na tom nezalezi.
 */
export const sync = $state({
	/** navigator.onLine sam o sebe klame (wifi bez internetu), takze ho oprava aj kazdy zapis. */
	online: true,
	/** Kolko zapisov caka v rade. */
	pending: 0,
	/** Prave sa odosiela. */
	sending: false,
	/** Kolko zapisov server odmietol - o tie sa uz nikto pokusat nebude. */
	rejected: 0
});
