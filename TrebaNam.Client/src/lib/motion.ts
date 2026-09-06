import { prefersReducedMotion } from 'svelte/motion';

/**
 * Dlzka animacie v milisekundach. Kto ma v systeme vypnute animacie, dostane nulu - pohyb je
 * ozdoba, obrazovka musi fungovat aj bez neho.
 */
export function ms(duration: number) {
	return prefersReducedMotion.current ? 0 : duration;
}
