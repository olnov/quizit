export const GameMode = {
	Competition: 0,
	Study: 1
} as const;

export type GameMode = (typeof GameMode)[keyof typeof GameMode];

export function isModeAvailableForRoom(gameMode: GameMode, solo: boolean): boolean {
	return solo ? gameMode === GameMode.Study : gameMode === GameMode.Competition;
}
