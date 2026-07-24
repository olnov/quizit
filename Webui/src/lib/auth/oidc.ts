import { browser } from '$app/environment';

type TokenSet = {
    accessToken: string;
    refreshToken: string | null;
    expiresAt: number;
};

const storageKey = 'quizit:admin-tokens';

function getClientId(): string {
    return import.meta.env.VITE_OIDC_CLIENT_ID ?? 'quizit-webui';
}

export async function signIn(username: string, password: string): Promise<void> {
    const response = await requestTokens({
        grant_type: 'password',
        client_id: getClientId(),
        username,
        password,
        scope: 'openid profile email roles offline_access quizit_api'
    });

    storeTokens(response);
}

export function signOut(): void {
    if (browser) {
        sessionStorage.removeItem(storageKey);
    }
}

export async function getAccessToken(): Promise<string | null> {
    const tokens = getStoredTokens();
    if (!tokens) {
        return null;
    }

    if (tokens.expiresAt > Date.now() + 15_000) {
        return tokens.accessToken;
    }

    if (!tokens.refreshToken) {
        signOut();
        return null;
    }

    try {
        const response = await requestTokens({
            grant_type: 'refresh_token',
            client_id: getClientId(),
            refresh_token: tokens.refreshToken
        });
        storeTokens(response, tokens.refreshToken);
        return response.access_token;
    } catch {
        signOut();
        return null;
    }
}

async function requestTokens(parameters: Record<string, string>): Promise<{
    access_token: string;
    refresh_token?: string;
    expires_in: number;
}> {
    const response = await fetch('/api/v1/connect/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams(parameters)
    });

    if (!response.ok) {
        throw new Error('Invalid username or password.');
    }

    return response.json();
}

function getStoredTokens(): TokenSet | null {
    if (!browser) {
        return null;
    }

    const value = sessionStorage.getItem(storageKey);
    if (!value) {
        return null;
    }

    try {
        return JSON.parse(value) as TokenSet;
    } catch {
        signOut();
        return null;
    }
}

function storeTokens(
    response: { access_token: string; refresh_token?: string; expires_in: number },
    previousRefreshToken: string | null = null
): void {
    if (!browser) {
        return;
    }

    const tokens: TokenSet = {
        accessToken: response.access_token,
        refreshToken: response.refresh_token ?? previousRefreshToken,
        expiresAt: Date.now() + response.expires_in * 1000
    };
    sessionStorage.setItem(storageKey, JSON.stringify(tokens));
}
