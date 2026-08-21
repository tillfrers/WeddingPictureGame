import { error } from '@sveltejs/kit';
import { apiErrorMessage, imageClient } from '$lib/api';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ params, url, depends }) => {
	// Manuell invalidierbar, damit wir nach einem Upload gezielt neu laden
	// können, ohne dass sich die URL ändert.
	depends('app:gallery');

	const tisch = params.tisch;
	const requestedPage = Number(url.searchParams.get('page') ?? '1');
	const page = Number.isFinite(requestedPage) && requestedPage > 0 ? Math.floor(requestedPage) : 1;

	try {
		const gallery = await imageClient.getGallery(tisch, page);
		return { tisch, page, gallery };
	} catch (err) {
		error(400, apiErrorMessage(err));
	}
};
