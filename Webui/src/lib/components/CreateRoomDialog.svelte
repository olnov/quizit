<script lang="ts">
	import { Dialog } from 'bits-ui';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import TextField from '$lib/components/ui/TextField.svelte';
	import { createRoom, createSoloRoom, getQuizzes, saveRoomSession } from '$lib/game-room';
	import { generateNickName } from '$lib/name-generator';

	let { solo = false }: { solo?: boolean } = $props();

	type Quiz = { id: string; title: string; questionsPerGame: number };

	let hostName = $state(generateNickName());
	let quizzes = $state<Quiz[]>([]);
	let quizId = $state('');
	let questionCount = $state(1);
	let answerTimeLimitSeconds = $state<number | null>(null);
	let questionSelectionMode = $state(0);
	let specificDifficulty = $state(100);
	let message = $state('');
	let creating = $state(false);

	onMount(async () => {
		try {
			quizzes = await getQuizzes();
			quizId = quizzes[0]?.id ?? '';
			questionCount = quizzes[0]?.questionsPerGame ?? 1;
			if (quizzes.length === 0) {
				message = 'There are no quizzes yet. Create a quiz before opening a room.';
			}
		} catch (error) {
			message = error instanceof Error ? error.message : 'The Quizz API is unavailable. Start the backend, then try again.';
		}
	});

	async function create() {
		if (!hostName.trim()) {
			message = 'Enter your name to create a room.';
			return;
		}

		if (!quizId) {
			message = 'There are no quizzes yet. Create a quiz before opening a room.';
			return;
		}

		creating = true;
		message = '';
		try {
			const createGame = solo ? createSoloRoom : createRoom;
			const response = await createGame(quizId, hostName.trim(), questionCount, answerTimeLimitSeconds, questionSelectionMode, questionSelectionMode === 1 ? specificDifficulty : null);
			saveRoomSession(response.room.gameCode, {
				...response.credentials,
				playerName: hostName.trim(),
				isHost: true
			});
			await goto(solo ? `/game/${response.room.gameCode}` : `/lobby/${response.room.gameCode}`);
		} catch (error) {
			message = error instanceof Error ? error.message : 'Unable to create the room.';
		} finally {
			creating = false;
		}
	}
</script>

<Dialog.Root>
	<Dialog.Trigger class="game-button">{solo ? 'Play solo' : 'Create a room'}</Dialog.Trigger>
	<Dialog.Portal>
		<Dialog.Overlay class="game-dialog-overlay" />
		<Dialog.Content class="game-dialog" aria-describedby="create-room-description">
			<div class="dialog-header">
				<div>
					<p class="eyebrow">New game</p>
					<Dialog.Title>{solo ? 'Play solo' : 'Create a room'}</Dialog.Title>
				</div>
				<Dialog.Close class="game-dialog-close" aria-label="Close dialog">&times;</Dialog.Close>
			</div>
			<Dialog.Description id="create-room-description">
				{solo
					? 'Choose a quiz and start playing immediately.'
					: 'Choose a quiz. Your lobby stays open for ten minutes, then closes automatically.'}
			</Dialog.Description>
			<form onsubmit={(event) => { event.preventDefault(); create(); }}>
				<TextField label="Your nickname" placeholder="e.g. BlueFish99" bind:value={hostName} />
				<label class="select-field">
					<span>Quiz</span>
					<select bind:value={quizId} disabled={quizzes.length === 0}>
						{#if quizzes.length === 0}
							<option>No quizzes available</option>
						{:else}
							{#each quizzes as quiz}
								<option value={quiz.id}>{quiz.title}</option>
							{/each}
						{/if}
					</select>
				</label>
				<label class="select-field">
					<span>Questions in this round</span>
					<input type="number" min="1" max={quizzes.find((quiz) => quiz.id === quizId)?.questionsPerGame ?? 1} bind:value={questionCount} />
				</label>
				<label class="select-field">
					<span>Answer time</span>
					<select value={answerTimeLimitSeconds ?? 'unlimited'} onchange={(event) => { const value = event.currentTarget.value; answerTimeLimitSeconds = value === 'unlimited' ? null : Number(value); }}>
						<option value="15">15 seconds</option>
						<option value="30">30 seconds</option>
						<option value="60">60 seconds</option>
						<option value="unlimited">Unlimited</option>
					</select>
				</label>
				<label class="select-field">
					<span>Question order</span>
					<select bind:value={questionSelectionMode}>
						<option value={0}>Ascending difficulty</option>
						<option value={1}>Specific difficulty</option>
						<option value={2}>Mixed</option>
					</select>
				</label>
				{#if questionSelectionMode === 1}
					<label class="select-field"><span>Difficulty (0-1000)</span><input type="number" min="0" max="1000" step="100" bind:value={specificDifficulty} /></label>
				{/if}
				<Button type="submit" class="submit-button" disabled={creating}>
					{creating ? 'Creating...' : solo ? 'Start solo game' : 'Create room'}
				</Button>
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
	select, input { appearance: none; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink); font: inherit; min-height: 46px; padding: 0 13px; }
	:global(.submit-button) { margin-top: 4px; width: 100%; }
	.message { color: var(--color-muted); font-size: 0.875rem; line-height: 1.4; margin: -4px 0 0; }
</style>
