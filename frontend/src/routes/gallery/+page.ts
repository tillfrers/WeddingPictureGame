import { error } from '@sveltejs/kit';
import { apiErrorMessage, apiErrorStatus, imageClient } from '$lib/api';
import type { PageLoad } from './$types';

// Galerie über alle Tische. Statische Route, gewinnt damit gegen /[tisch] -
// Tischcodes sind ohnehin vierstellige Hex-Codes und kollidieren nicht.
export const load: PageLoad = async ({ url, depends, parent }) => {
	depends('app:gallery');

	// Siehe /[tisch]/+page.ts: erst Token einlösen, dann laden.
	await parent();

	const requestedPage = Number(url.searchParams.get('page') ?? '1');
	const page = Number.isFinite(requestedPage) && requestedPage > 0 ? Math.floor(requestedPage) : 1;

	try {
		const gallery = await imageClient.getGalleryAll(page);
		return { page, gallery };
	} catch (err) {
		error(apiErrorStatus(err), apiErrorMessage(err));
	}
};
