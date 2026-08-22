import { redirect } from '@sveltejs/kit';
import type { PageLoad } from './$types';

// Zweite akzeptierte QR-Code-Form: der Tischcode steckt in der Query
// (/?tisch=9E79&token=...) statt im Pfad (/9E79?token=...). Das Token hat der
// Layout-Load zu diesem Zeitpunkt bereits eingelöst.
export const load: PageLoad = async ({ url, parent }) => {
	await parent();

	const tisch = url.searchParams.get('tisch');

	if (tisch) redirect(307, `/${encodeURIComponent(tisch)}`);
};
