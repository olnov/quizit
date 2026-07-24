import { env } from '$env/dynamic/private';

export function GET(): Response {
    const backendPublicUrl = env.BACKEND_PUBLIC_URL?.replace(/\/$/, '');
    if (!backendPublicUrl) {
        return Response.json({ message: 'BACKEND_PUBLIC_URL is not configured.' }, { status: 500 });
    }

    return Response.json({ backendPublicUrl });
}
