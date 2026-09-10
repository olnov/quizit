<script lang="ts">
	import type { RoomPlayer } from '$lib/game-room';

	let {
		players,
		getStatus
	}: {
		players: RoomPlayer[];
		getStatus?: (player: RoomPlayer) => string | undefined;
	} = $props();

	let container = $state<HTMLUListElement>();
	let hasMoreBelow = $state(false);

	function updateScrollState() {
		if (!container) return;

		hasMoreBelow =
			container.scrollHeight > container.clientHeight &&
			container.scrollTop + container.clientHeight < container.scrollHeight - 2;
	}

	function scheduleScrollStateUpdate() {
		requestAnimationFrame(() => requestAnimationFrame(updateScrollState));
	}

	$effect(() => {
		players.length;
		const list = container;
		if (!list) return;

		const observer = new ResizeObserver(scheduleScrollStateUpdate);
		observer.observe(list);
		scheduleScrollStateUpdate();

		return () => observer.disconnect();
	});
</script>

<div class="player-list-wrap">
	<ul bind:this={container} class="player-list" onscroll={updateScrollState}>
		{#each players as player}
			{@const status = getStatus?.(player)}
			<li>
				<span class:offline={!player.isConnected} class="player-status" aria-hidden="true"></span>
				<span>{player.name}</span>
				{#if status}<small>{status}</small>{/if}
			</li>
		{/each}
	</ul>
	{#if hasMoreBelow}
		<div class="more-indicator" aria-hidden="true">More players below &#8595;</div>
	{/if}
</div>

<style>
	.player-list-wrap {
		flex: 1 1 auto;
		margin-top: 20px;
		min-height: 0;
		position: relative;
	}
	.player-list {
		align-content: start;
		display: grid;
		gap: 3px;
		height: 100%;
		list-style: none;
		margin: 0;
		overflow-y: auto;
		padding: 0;
		scrollbar-width: none;
	}
	.player-list::-webkit-scrollbar {
		display: none;
	}
	.player-list li {
		align-items: center;
		border-bottom: 1px solid #3f444d;
		display: flex;
		gap: 11px;
		min-height: 53px;
	}
	.player-status {
		background: #8bbb4c;
		border-radius: 50%;
		height: 8px;
		width: 8px;
	}
	.player-status.offline {
		background: #858995;
	}
	.player-list small {
		color: #b8bbc5;
		font-size: 0.72rem;
		margin-left: auto;
	}
	.more-indicator {
		bottom: 0;
		color: var(--color-lime);
		font-size: 0.67rem;
		font-weight: 800;
		left: 0;
		letter-spacing: 0.08em;
		padding: 7px 16px;
		pointer-events: none;
		position: absolute;
		right: 0;
		text-align: center;
		text-transform: uppercase;
	}
</style>
