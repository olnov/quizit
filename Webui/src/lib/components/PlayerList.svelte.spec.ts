import { page } from 'vitest/browser';
import { describe, expect, it } from 'vitest';
import { render } from 'vitest-browser-svelte';
import PlayerList from './PlayerList.svelte';

describe('PlayerList.svelte', () => {
	it('renders players and their page-specific status', async () => {
		render(PlayerList, {
			players: [
				{ playerId: 'host', name: 'Host player', score: 0, isConnected: true, hasAnswered: false },
				{ playerId: 'guest', name: 'Guest player', score: 0, isConnected: false, hasAnswered: true }
			],
			getStatus: (player) => (player.playerId === 'host' ? 'Host' : 'Disconnected')
		});

		await expect.element(page.getByRole('list')).toBeInTheDocument();
		await expect.element(page.getByText('Host player')).toBeInTheDocument();
		await expect.element(page.getByText('Guest player')).toBeInTheDocument();
		await expect.element(page.getByText('Host')).toBeInTheDocument();
		await expect.element(page.getByText('Disconnected')).toBeInTheDocument();
	});
});
