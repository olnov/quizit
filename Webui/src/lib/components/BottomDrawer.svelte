<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		open = $bindable(false),
		title,
		children
	}: {
		open?: boolean;
		title: string;
		children: Snippet;
	} = $props();

	function close() {
		open = false;
	}

	function handleKeydown(event: KeyboardEvent) {
		if (open && event.key === 'Escape') close();
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if open}
	<div class="drawer-overlay" role="presentation" onclick={close}></div>
	<dialog class="drawer" open aria-modal="true" aria-label={title}>
		<div class="drawer-handle" aria-hidden="true"></div>
		<header>
			<div>
				<p class="eyebrow">Learning note</p>
				<h2>{title}</h2>
			</div>
			<button class="drawer-close" type="button" aria-label="Close explanation" onclick={close}>
				&times;
			</button>
		</header>
		<div class="drawer-content">{@render children()}</div>
		<button class="game-button drawer-confirm" type="button" onclick={close}>OK</button>
	</dialog>
{/if}

<style>
	.drawer-overlay {
		background: rgb(25 29 40 / 45%);
		inset: 0;
		position: fixed;
		z-index: 90;
	}
	.drawer {
		background: var(--color-surface);
		border: 2px solid var(--color-ink);
		bottom: 0;
		box-shadow: 0 -8px 0 var(--color-lime);
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
		left: 0;
		max-height: 38dvh;
		margin: 0;
		max-width: none;
		padding: 14px 24px 20px;
		position: fixed;
		right: 0;
		width: 100%;
		z-index: 91;
	}
	.drawer-handle {
		background: var(--color-border);
		border-radius: 999px;
		height: 4px;
		margin: 0 auto 12px;
		width: 48px;
	}
	header {
		align-items: flex-start;
		display: flex;
		justify-content: space-between;
	}
	h2 {
		font-family: var(--font-display);
		font-size: 1.8rem;
		font-weight: 400;
		line-height: 1;
		margin: 5px 0 0;
	}
	.drawer-close {
		background: transparent;
		border: 0;
		color: var(--color-muted);
		cursor: pointer;
		font-size: 2rem;
		line-height: 1;
		padding: 0 4px;
	}
	.drawer-content {
		color: var(--color-muted);
		line-height: 1.6;
		min-height: 0;
		overflow-y: auto;
		padding: 14px 8px 0 0;
	}
	.drawer-confirm {
		align-self: flex-end;
		margin-top: 14px;
	}
	@media (max-width: 800px) {
		.drawer { max-height: 68dvh; padding-inline: 20px; }
	}
</style>
