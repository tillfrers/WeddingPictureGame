<script lang="ts">
	import GalleryView from '$lib/components/GalleryView.svelte';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();
</script>

<GalleryView tisch={null} page={data.page} gallery={data.gallery} />
