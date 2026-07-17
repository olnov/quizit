<script lang="ts">
	import { Dialog } from 'bits-ui';
	import Button from '$lib/components/ui/Button.svelte';
	import TextField from '$lib/components/ui/TextField.svelte';

	let playerName = $state('');
	let gameCode = $state('');
	let message = $state('');

	function joinRoom() {
		message = playerName.trim() && gameCode.trim()
			? 'Room connection will be available when the backend is connected.'
			: 'Enter your name and the six-character room code.';
	}
</script>

<Dialog.Root>
	<Dialog.Trigger class="game-button">I have a room code</Dialog.Trigger>
	<Dialog.Portal>
		<Dialog.Overlay class="game-dialog-overlay" />
		<Dialog.Content class="game-dialog" aria-describedby="join-room-description">
			<div class="dialog-header">
				<div>
					<p class="eyebrow">Join a game</p>
					<Dialog.Title>Enter your room</Dialog.Title>
				</div>
				<Dialog.Close class="game-dialog-close" aria-label="Close dialog">&times;</Dialog.Close>
			</div>
			<Dialog.Description id="join-room-description">
				Ask the host for the six-character code, then choose the name shown to other players.
			</Dialog.Description>
			<form onsubmit={(event) => { event.preventDefault(); joinRoom(); }}>
				<TextField label="Your name" placeholder="e.g. Alex" bind:value={playerName} />
				<TextField label="Room code" placeholder="ABC123" maxlength={6} className="code-field" bind:value={gameCode} />
				<Button type="submit" class="submit-button">Join room</Button>
				{#if message}<p class="message" aria-live="polite">{message}</p>{/if}
			</form>
		</Dialog.Content>
	</Dialog.Portal>
</Dialog.Root>

<style>
	.dialog-header { align-items: flex-start; display: flex; justify-content: space-between; margin-bottom: 10px; }
	.dialog-header :global(h2) { font-family: var(--font-display); font-size: 2rem; line-height: 1.05; margin: 4px 0 0; }
	:global(.game-dialog #join-room-description) { color: var(--color-muted); line-height: 1.55; margin: 0 0 24px; }
	form { display: grid; gap: 18px; }
	:global(.code-field input) { letter-spacing: .12em; text-transform: uppercase; }
	:global(.submit-button) { margin-top: 4px; width: 100%; }
	.message { color: var(--color-muted); font-size: .875rem; line-height: 1.4; margin: -4px 0 0; }
</style>
