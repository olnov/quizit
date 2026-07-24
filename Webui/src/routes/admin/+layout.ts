import { redirect } from '@sveltejs/kit';
import { getAccessToken } from '$lib/auth/oidc';

export const ssr = false;

export async function load({ url }) {
    const isPublicAuthRoute = url.pathname === '/admin/login'
        || url.pathname.startsWith('/admin/auth/');

    if (isPublicAuthRoute) {
        return;
    }

    if (!await getAccessToken()) {
        throw redirect(303, `/admin/login?returnTo=${encodeURIComponent(url.pathname)}`);
    }
}
