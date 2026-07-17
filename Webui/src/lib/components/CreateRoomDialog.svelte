<script lang="ts">
	import { Dialog } from 'bits-ui';
	import Button from '$lib/components/ui/Button.svelte';
	import TextField from '$lib/components/ui/TextField.svelte';

	let hostName = $state('');
	let theme = $state('General knowledge');
	let message = $state('');

	function createRoom() {
		message = hostName.trim()
			? `Room setup for ${hostName.trim()} will be connected to the Quizz API next.`
			: 'Enter your name before creating the room.';
	}
</script>

<Dialog.Root>
	<Dialog.Trigger class="game-button">Create a room <span aria-hidden="true">&rarr;</span></Dialog.Trigger>
	<Dialog.Portal>
		<Dialog.Overlay class="game-dialog-overlay" />
		<Dialog.Content class="game-dialog" aria-describedby="create-room-description">
			<div class="dialog-header">
				<div>
					<p class="eyebrow">New game</p>
					<Dialog.Title>Create a room</Dialog.Title>
				</div>
				<Dialog.Close class="game-dialog-close" aria-label="Close dialog">&times;</Dialog.Close>
			</div>
			<Dialog.Description id="create-room-description">
				Choose a topic now. The room code is created once the backend request is connected.
			</Dialog.Description>
			<form onsubmit={(event) => { event.preventDefault(); createRoom(); }}>
				<TextField label="Your name" placeholder="e.g. Alex" bind:value={hostName} />
				<label class="select-field">
					<span>Quiz theme</span>
					<select bind:value={theme}>
						<option>General knowledge</option>
						<option>Science &amp; discovery</option>
						<option>History</option>
						<option>Film &amp; music</option>
					</select>
				</label>
				<Button type="submit" class="submit-button">Create room</Button>
				{#if message}<p class="message" aria-live="polite">{message}</p>{/if}
			</form>
		</Dialog.Content>
	</Dialog.Portal>
</Dialog.Root>

<style>
	.dialog-header { align-items: flex-start; display: flex; justify-content: space-between; margin-bottom: 10px; }
	.dialog-header :global(h2) { font-family: var(--font-display); font-size: 2rem; line-height: 1.05; margin: 4px 0 0; }
	:global(.game-dialog #create-room-description) { color: var(--color-muted); line-height: 1.55; margin: 0 0 24px; }
	form { display: grid; gap: 18px; }
	.select-field { color: var(--color-ink); display: grid; font-size: 0.875rem; font-weight: 700; gap: 7px; }
	select { appearance: none; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 46px; padding: 0 13px; }
	:global(.submit-button) { margin-top: 4px; width: 100%; }
	.message { color: var(--color-muted); font-size: 0.875rem; line-height: 1.4; margin: -4px 0 0; }
</style>
