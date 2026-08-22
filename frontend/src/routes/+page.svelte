<script lang="ts">
	import { fade } from 'svelte/transition';
</script>

<main in:fade={{ duration: 250 }}>
	<div class="card">
		<span class="emoji">💍</span>
		<h1>Hochzeits-Fotobox</h1>
		<p>
			Scanne den QR-Code an deinem Tisch, um die Fotogalerie zu öffnen und eigene Bilder
			hochzuladen.
		</p>
	</div>
</main>

<style>
	/* Feste Hoehe statt min-height, weil body nicht mehr scrollt (app.css):
	   nur so kann dieser Container ueberlangen Inhalt selbst scrollen.
	   Zentriert wird ueber margin:auto der Karte statt align-items, sonst
	   schneidet Flexbox bei Ueberlaenge den oberen Rand ab. */
	main {
		height: 100dvh;
		overflow-y: auto;
		display: flex;
		padding: 24px calc(24px + var(--safe-right)) 24px calc(24px + var(--safe-left));
	}

	.card {
		margin: auto;
		max-width: 380px;
		text-align: center;
		background: var(--color-surface);
		border-radius: var(--radius-l);
		box-shadow: var(--shadow-soft);
		padding: 40px 28px;
	}

	.emoji {
		font-size: 48px;
		display: block;
		margin-bottom: 12px;
	}

	h1 {
		margin: 0 0 12px;
		font-size: 1.5rem;
	}

	p {
		margin: 0;
		color: var(--color-text-muted);
		line-height: 1.5;
	}
</style>
