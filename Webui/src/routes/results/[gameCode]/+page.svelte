<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { connectToRoom, getGameResult, getRoom, getRoomSession, restartRound, saveRoomSession, type ScoreboardPlayer } from '$lib/game-room';

	let { params } = $props();
	let players = $state<ScoreboardPlayer[]>([]);
	let isHost = $state(false);
	let token = $state('');
	let message = $state('Loading results...');

	onMount(async () => {
		const session = getRoomSession(params.gameCode);
		if (!session) { await goto('/'); return; }
		isHost = session.isHost;
		token = session.playerToken;
		void connectToRoom(
			params.gameCode,
			session,
			(updatedRoom) => { if (updatedRoom.status === 0) void goto(`/lobby/${params.gameCode}`); },
			(credentials) => { saveRoomSession(params.gameCode, { ...session, ...credentials }); }
		);
		const result = getGameResult(params.gameCode);
		if (result) { players = result.players; message = ''; return; }
		try { players = (await getRoom(params.gameCode)).players.map((player) => ({ playerId: player.playerId, name: player.name, score: player.score })); message = ''; }
		catch (error) { message = error instanceof Error ? error.message : 'Unable to load the results.'; }
	});

	async function playAgain() {
		try { await restartRound(params.gameCode, token); await goto(`/lobby/${params.gameCode}`); }
		catch (error) { message = error instanceof Error ? error.message : 'Unable to restart the round.'; }
	}
</script>

<main class="results-shell"><a class="brand" href="/"><span class="brand-mark">Q</span><span>QuizIt</span></a><section>{#if players.length}<p class="eyebrow">Round complete</p><h1>Final results</h1><ol>{#each [...players].sort((a, b) => b.score - a.score) as player, index}<li><span class="rank">{index + 1}</span><strong>{player.name}</strong><span>{player.score} pts</span></li>{/each}</ol>{#if isHost}<Button onclick={playAgain}>Play another round</Button>{/if}<a class="home-link" href="/">Finish and return home</a>{:else}<p>{message}</p>{/if}</section></main>

<style>
	.results-shell { margin: 0 auto; max-width: 900px; min-height: 100vh; padding: 34px 48px; }
	section { padding-top: 92px; } h1 { font-family: var(--font-display); font-size: clamp(3.5rem, 8vw, 7rem); font-weight: 400; line-height: .9; margin: 16px 0 46px; }
	ol { list-style: none; margin: 0 0 34px; padding: 0; } li { align-items: center; border-top: 1px solid var(--color-border); display: flex; gap: 18px; min-height: 70px; } li:last-child { border-bottom: 1px solid var(--color-border); } .rank { color: var(--color-accent); font-size: 1.1rem; font-weight: 800; width: 28px; } li strong { font-size: 1.15rem; } li > span:last-child { color: var(--color-muted); margin-left: auto; } .home-link { display: block; font-weight: 700; margin-top: 30px; } @media (max-width: 600px) { .results-shell { padding: 26px 20px; } section { padding-top: 64px; } }
</style>
