// Verejne stranky idu do statickeho exportu; /app/* si to prepina na false,
// pretoze bezi ako SPA a data si taha z API az v prehliadaci.
export const prerender = true;

// Kazda stranka sa vygeneruje ako adresar s index.html, takze staticky hosting
// obsluzi aj /cesta, aj /cesta/ - a canonical odkazy vyzeraju rovnako.
export const trailingSlash = 'always';
