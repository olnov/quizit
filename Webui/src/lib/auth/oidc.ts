import { browser } from '$app/environment';
import { UserManager, WebStorageStateStore, type User } from 'oidc-client-ts';

let manager: UserManager | undefined;

function getAuthority(): string {
    return (import.meta.env.VITE_OIDC_AUTHORITY ?? import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5298')
        .replace(/\/$/, '');
}

function getClientId(): string {
    return import.meta.env.VITE_OIDC_CLIENT_ID ?? 'quizit-webui';
}

function getManager(): UserManager {
    if (!browser) {
        throw new Error('OIDC authentication is only available in the browser.');
    }

    if (manager) {
        return manager;
    }

    manager = new UserManager({
        authority: getAuthority(),
        client_id: getClientId(),
        redirect_uri: `${window.location.origin}/admin/auth/callback`,
        post_logout_redirect_uri: `${window.location.origin}/admin/login`,
        response_type: 'code',
        scope: 'openid profile email roles offline_access quizit_api',
        userStore: new WebStorageStateStore({ store: window.sessionStorage }),
    });

    return manager;
}

export async function signIn(): Promise<void> {
    await getManager().signinRedirect();
}

export async function completeSignIn(): Promise<User> {
    return getManager().signinRedirectCallback();
}

export async function signOut(): Promise<void> {
    await getManager().signoutRedirect();
}

export async function getAccessToken(): Promise<string | null> {
    const user = await getManager().getUser();
    if (!user) {
        return null;
    }

    if (!user.expired) {
        return user.access_token;
    }

    if (!user.refresh_token) {
        await getManager().removeUser();
        return null;
    }

    const response = await fetch(`${getAuthority()}/connect/token`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            grant_type: 'refresh_token',
            client_id: getClientId(),
            refresh_token: user.refresh_token,
        }),
    });

    if (!response.ok) {
        await getManager().removeUser();
        return null;
    }

    const tokens: {
        access_token: string;
        expires_in: number;
        refresh_token?: string;
    } = await response.json();

    user.access_token = tokens.access_token;
    user.refresh_token = tokens.refresh_token ?? user.refresh_token;
    user.expires_at = Math.floor(Date.now() / 1000) + tokens.expires_in;
    await getManager().storeUser(user);

    return user.access_token;
}
