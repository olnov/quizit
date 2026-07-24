import { env } from '$env/dynamic/private';
import type { RequestHandler } from './$types';

const proxy: RequestHandler = async ({ params, request }) => {
    const backendUrl = env.BACKEND_API_URL?.replace(/\/$/, '');
    if (!backendUrl) {
        return Response.json({ message: 'BACKEND_API_URL is not configured.' }, { status: 500 });
    }

    const targetUrl = new URL(`/api/${params.path}`, backendUrl);
    targetUrl.search = new URL(request.url).search;
    const headers = new Headers(request.headers);
    headers.delete('host');
    headers.delete('connection');
    headers.delete('content-length');

    try {
        const response = await fetch(targetUrl, {
            method: request.method,
            headers,
            body: request.method === 'GET' || request.method === 'HEAD'
                ? undefined
                : await request.arrayBuffer(),
            redirect: 'manual'
        });

        const responseHeaders = new Headers(response.headers);
        responseHeaders.delete('connection');
        responseHeaders.delete('transfer-encoding');

        return new Response(response.body, {
            status: response.status,
            statusText: response.statusText,
            headers: responseHeaders
        });
    } catch {
        return Response.json({ message: 'The backend service is unavailable.' }, { status: 502 });
    }
};

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;
