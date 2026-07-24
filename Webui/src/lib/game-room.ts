import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';

const apiBaseUrl = '';
let backendPublicUrl: Promise<string> | undefined;

export type RoomPlayer = { playerId: string; name: string; score: number; isConnected: boolean; hasAnswered: boolean };
export type GameRoom = { gameCode: string; quizId: string; status: number; lobbyExpiresAt: string; questionCount: number; answerTimeLimitSeconds: number | null; questionSelectionMode: number; specificDifficulty: number | null; answerDeadlineAt: string | null; currentQuestionIndex: number; players: RoomPlayer[] };
export type AnswerOption = { id: string; text: string };
export type CurrentQuestion = { index: number; answerDeadlineAt: string | null; question: { id: string; text: string; codeContext: string | null; difficulty: number; options: AnswerOption[] } };
export type Reveal = { questionId: string; correctOptionId: string; explanation: string | null };
export type ScoreboardPlayer = { playerId: string; name: string; score: number };
export type GameCompleted = { gameCode: string; players: ScoreboardPlayer[] };
export type PlayerCredentials = { playerId: string; playerToken: string };
export type RoomSession = PlayerCredentials & { playerName: string; isHost: boolean };

type CreateRoomResponse = { room: GameRoom; credentials: PlayerCredentials };
type JoinGameResponse = CreateRoomResponse;
type ApiError = { message?: string };

export type RoomEventHandlers = {
	onGameStarted?: (room: GameRoom) => void;
	onQuestionStarted?: (question: CurrentQuestion) => void;
	onQuestionRevealed?: (reveal: Reveal) => void;
	onScoreboardUpdated?: (scoreboard: { gameCode: string; players: ScoreboardPlayer[] }) => void;
	onGameCompleted?: (completed: GameCompleted) => void;
	onRoomUpdated?: (room: GameRoom) => void;
};

export async function createRoom(quizId: string, hostName: string, questionCount: number, answerTimeLimitSeconds: number | null, questionSelectionMode: number, specificDifficulty: number | null): Promise<CreateRoomResponse> {
	return request<CreateRoomResponse>('/api/v1/game-rooms', { method: 'POST', body: JSON.stringify({ quizId, hostName, questionCount, answerTimeLimitSeconds, questionSelectionMode, specificDifficulty }) });
}

export async function createSoloRoom(quizId: string, playerName: string, questionCount: number, answerTimeLimitSeconds: number | null, questionSelectionMode: number, specificDifficulty: number | null): Promise<CreateRoomResponse> {
	return request<CreateRoomResponse>('/api/v1/game-rooms/solo', { method: 'POST', body: JSON.stringify({ quizId, hostName: playerName, questionCount, answerTimeLimitSeconds, questionSelectionMode, specificDifficulty }) });
}

export async function getQuizzes(): Promise<Array<{ id: string; title: string; questionsPerGame: number }>> {
	return request<Array<{ id: string; title: string; questionsPerGame: number }>>('/api/v1/quizes', { method: 'GET' });
}
export async function getQuizDifficultyCounts(quizId: string): Promise<Array<{ difficulty: number; count: number }>> { return request<Array<{ difficulty: number; count: number }>>(`/api/v1/quizes/${encodeURIComponent(quizId)}/difficulty-counts`, { method: 'GET' }); }

export async function getRoom(gameCode: string): Promise<GameRoom> {
	return request<GameRoom>(`/api/v1/game-rooms?gameCode=${encodeURIComponent(gameCode)}`, { method: 'GET' });
}

export async function startRoom(gameCode: string, playerToken: string): Promise<GameRoom> { return tokenRequest<GameRoom>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/start`, playerToken); }
export async function updateRoomSettings(gameCode: string, playerToken: string, questionCount: number, answerTimeLimitSeconds: number | null, questionSelectionMode: number, specificDifficulty: number | null): Promise<GameRoom> { return request<GameRoom>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/settings`, { method: 'POST', body: JSON.stringify({ playerToken, questionCount, answerTimeLimitSeconds, questionSelectionMode, specificDifficulty }) }); }
export async function submitAnswer(gameCode: string, playerToken: string, answerOptionId: string): Promise<void> { await request(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/answers`, { method: 'POST', body: JSON.stringify({ playerToken, answerOptionId }) }); }
export async function nextQuestion(gameCode: string, playerToken: string): Promise<void> { await tokenRequest(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/next`, playerToken); }
export async function completeGame(gameCode: string, playerToken: string): Promise<GameCompleted> { return tokenRequest<GameCompleted>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/complete`, playerToken); }
export async function restartRound(gameCode: string, playerToken: string): Promise<GameRoom> { return tokenRequest<GameRoom>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/restart`, playerToken); }
export async function getCurrentQuestion(gameCode: string): Promise<CurrentQuestion> { return request<CurrentQuestion>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/questions/current`, { method: 'GET' }); }

export async function connectToRoom(gameCode: string, session: RoomSession, onLobbyUpdated: (room: GameRoom) => void, onCredentialsUpdated: (credentials: PlayerCredentials) => void, handlers: RoomEventHandlers = {}): Promise<HubConnection> {
	const connection = new HubConnectionBuilder().withUrl(`${await getBackendPublicUrl()}/api/v1/hubs/game`).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();
	const join = async () => {
		const response = await connection.invoke<JoinGameResponse>('JoinGame', { gameCode, playerName: session.playerName, playerToken: session.playerToken || null });
		onLobbyUpdated(response.room);
		onCredentialsUpdated(response.credentials);
	};
	connection.on('LobbyUpdated', onLobbyUpdated);
	if (handlers.onGameStarted) connection.on('GameStarted', handlers.onGameStarted);
	if (handlers.onQuestionStarted) connection.on('QuestionStarted', handlers.onQuestionStarted);
	if (handlers.onQuestionRevealed) connection.on('QuestionRevealed', handlers.onQuestionRevealed);
	if (handlers.onScoreboardUpdated) connection.on('ScoreboardUpdated', handlers.onScoreboardUpdated);
	if (handlers.onGameCompleted) connection.on('GameCompleted', handlers.onGameCompleted);
	if (handlers.onRoomUpdated) connection.on('RoomUpdated', handlers.onRoomUpdated);
	connection.onreconnected(join);
	await connection.start();
	await join();
	return connection;
}

export function saveRoomSession(gameCode: string, session: RoomSession): void { sessionStorage.setItem(`quizit:room:${gameCode}`, JSON.stringify(session)); }
export function getRoomSession(gameCode: string): RoomSession | null { const value = sessionStorage.getItem(`quizit:room:${gameCode}`); if (!value) return null; try { return JSON.parse(value) as RoomSession; } catch { sessionStorage.removeItem(`quizit:room:${gameCode}`); return null; } }
export function saveGameResult(result: GameCompleted): void { sessionStorage.setItem(`quizit:result:${result.gameCode}`, JSON.stringify(result)); }
export function getGameResult(gameCode: string): GameCompleted | null { const value = sessionStorage.getItem(`quizit:result:${gameCode}`); if (!value) return null; try { return JSON.parse(value) as GameCompleted; } catch { return null; } }

function tokenRequest<T>(path: string, playerToken: string): Promise<T> { return request<T>(path, { method: 'POST', body: JSON.stringify({ playerToken }) }); }
async function getBackendPublicUrl(): Promise<string> {
	backendPublicUrl ??= fetch('/api/runtime-config')
		.then(async (response) => {
			if (!response.ok) throw new Error('The realtime backend URL is not configured.');
			return (await response.json() as { backendPublicUrl: string }).backendPublicUrl;
		});
	return backendPublicUrl;
}
async function request<T = void>(path: string, init: RequestInit): Promise<T> {
	const response = await fetch(`${apiBaseUrl}${path}`, { headers: { 'Content-Type': 'application/json' }, ...init });
	if (!response.ok) { const error = (await response.json().catch(() => ({}))) as ApiError; throw new Error(error.message ?? 'The request could not be completed.'); }
	return response.json() as Promise<T>;
}
