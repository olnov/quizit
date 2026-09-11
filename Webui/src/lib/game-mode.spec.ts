import { describe, expect, it } from 'vitest';
import { GameMode, isModeAvailableForRoom } from './game-mode';

describe('isModeAvailableForRoom', () => {
	it('offers Study quizzes only for solo games', () => {
		expect(isModeAvailableForRoom(GameMode.Study, true)).toBe(true);
		expect(isModeAvailableForRoom(GameMode.Study, false)).toBe(false);
	});

	it('offers Competition quizzes only for multiplayer rooms', () => {
		expect(isModeAvailableForRoom(GameMode.Competition, false)).toBe(true);
		expect(isModeAvailableForRoom(GameMode.Competition, true)).toBe(false);
	});
});
