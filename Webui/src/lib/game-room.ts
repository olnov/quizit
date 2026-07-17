import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';

const apiBaseUrl = import.meta.env.PUBLIC_API_BASE_URL ?? 'http://localhost:5298';

export type RoomPlayer = {
	playerId: string;
	name: string;
	score: number;
	isConnected: boolean;
};

export type GameRoom = {
	gameCode: string;
	quizId: string;
	status: number;
	lobbyExpiresAt: string;
	currentQuestionIndex: number;
	players: RoomPlayer[];
};

export type PlayerCredentials = {
	playerId: string;
	playerToken: string;
};

export type RoomSession = PlayerCredentials & {
	playerName: string;
	isHost: boolean;
};

type CreateRoomResponse = {
	room: GameRoom;
	credentials: PlayerCredentials;
};

type JoinGameResponse = CreateRoomResponse;

type ApiError = { message?: string };

export async function createRoom(quizId: string, hostName: string): Promise<CreateRoomResponse> {
	return request<CreateRoomResponse>('/api/v1/game-rooms', {
		method: 'POST',
		body: JSON.stringify({ quizId, hostName })
	});
}

export async function getQuizzes(): Promise<Array<{ id: string; title: string }>> {
	return request<Array<{ id: string; title: string }>>('/api/v1/quizes', { method: 'GET' });
}

export async function startRoom(gameCode: string, playerToken: string): Promise<GameRoom> {
	return request<GameRoom>(`/api/v1/game-rooms/${encodeURIComponent(gameCode)}/start`, {
		method: 'POST',
		body: JSON.stringify({ playerToken })
	});
}

export async function connectToRoom(
	gameCode: string,
	session: RoomSession,
	onLobbyUpdated: (room: GameRoom) => void,
	onCredentialsUpdated: (credentials: PlayerCredentials) => void
): Promise<HubConnection> {
	const connection = new HubConnectionBuilder()
		.withUrl(`${apiBaseUrl}/api/v1/hubs/game`)
		.withAutomaticReconnect()
		.configureLogging(LogLevel.Warning)
		.build();

	const join = async () => {
		const response = await connection.invoke<JoinGameResponse>('JoinGame', {
			gameCode,
			playerName: session.playerName,
			playerToken: session.playerToken || null
		});
		onLobbyUpdated(response.room);
		onCredentialsUpdated(response.credentials);
	};

	connection.on('LobbyUpdated', onLobbyUpdated);
	connection.onreconnected(join);

	await connection.start();
	await join();

	return connection;
}

export function saveRoomSession(gameCode: string, session: RoomSession): void {
	sessionStorage.setItem(`quizit:room:${gameCode}`, JSON.stringify(session));
}

export function getRoomSession(gameCode: string): RoomSession | null {
	const value = sessionStorage.getItem(`quizit:room:${gameCode}`);
	if (!value) {
		return null;
	}

	try {
		return JSON.parse(value) as RoomSession;
	} catch {
		sessionStorage.removeItem(`quizit:room:${gameCode}`);
		return null;
	}
}

async function request<T>(path: string, init: RequestInit): Promise<T> {
	const response = await fetch(`${apiBaseUrl}${path}`, {
		headers: { 'Content-Type': 'application/json' },
		...init
	});

	if (!response.ok) {
		const error = (await response.json().catch(() => ({}))) as ApiError;
		throw new Error(error.message ?? 'The request could not be completed.');
	}

	return response.json() as Promise<T>;
}
