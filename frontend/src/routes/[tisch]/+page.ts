import { error } from '@sveltejs/kit';
import { apiErrorMessage, apiErrorStatus, imageClient } from '$lib/api';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ params, url, depends, parent }) => {
	// Manuell invalidierbar, damit wir nach einem Upload gezielt neu laden
	// können, ohne dass sich die URL ändert.
	depends('app:gallery');

	// Wartet auf den Layout-Load, der ein Token aus der URL einlöst - sonst
	// liefe die Galerie-Abfrage ohne Auth-Cookie in einen 401.
	await parent();

	const tisch = params.tisch;
	const requestedPage = Number(url.searchParams.get('page') ?? '1');
	const page = Number.isFinite(requestedPage) && requestedPage > 0 ? Math.floor(requestedPage) : 1;

	try {
		const gallery = await imageClient.getGallery(tisch, page);
		return { tisch, page, gallery };
	} catch (err) {
		error(apiErrorStatus(err), apiErrorMessage(err));
	}
};
