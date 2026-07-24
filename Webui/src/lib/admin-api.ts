import { getAccessToken } from '$lib/auth/oidc';

const apiBaseUrl = '';

export type QuizStatus = 0 | 1 | 2;

export type QuizTheme = {
    id: string;
    name: string;
};

export type QuizListItem = {
    id: string;
    title: string;
    themeId: string;
    themeName: string;
    questionsPerGame: number;
    questionCount: number;
    status: QuizStatus;
    createdAt: string;
    updatedAt: string;
};

export type AdminQuestion = {
    id: string;
    text: string;
    codeContext: string | null;
    explanation: string | null;
    difficulty: number;
    options: string[];
    correctOptionIndex: number;
};

export type AdminQuiz = {
    id: string;
    title: string;
    themeId: string;
    themeName: string;
    questionsPerGame: number;
    status: QuizStatus;
    createdAt: string;
    updatedAt: string;
    questions: AdminQuestion[];
};

export type PagedResult<T> = {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
};

export type QuizImportDocument = {
    schemaVersion: number;
    theme: string;
    quiz: { title: string; questionsPerGame: number };
    questions: Array<{
        text: string;
        codeContext: string | null;
        explanation: string | null;
        difficulty: number;
        options: string[];
        correctOptionIndex: number;
    }>;
};

export type ImportValidation = {
    isValid: boolean;
    errors: string[];
    preview: {
        theme: string;
        title: string;
        questionsPerGame: number;
        questionCount: number;
    } | null;
};

export const quizStatusLabel: Record<QuizStatus, string> = {
    0: 'Draft',
    1: 'Published',
    2: 'Archived'
};

export async function getAdminQuizzes(page = 1, pageSize = 100): Promise<PagedResult<QuizListItem>> {
    return adminRequest(`/api/v1/admin/quizes?page=${page}&pageSize=${pageSize}`);
}

export async function getAdminQuiz(quizId: string): Promise<AdminQuiz> {
    return adminRequest(`/api/v1/admin/quizes/${encodeURIComponent(quizId)}`);
}

export async function getQuizThemes(): Promise<QuizTheme[]> {
    return adminRequest('/api/v1/admin/quiz-themes');
}

export async function createQuizTheme(name: string): Promise<QuizTheme> {
    return adminRequest('/api/v1/admin/quiz-themes', {
        method: 'POST',
        body: JSON.stringify({ name })
    });
}

export async function createAdminQuiz(input: {
    title: string;
    themeId: string;
    questionsPerGame: number;
}): Promise<AdminQuiz> {
    return adminRequest('/api/v1/admin/quizes', { method: 'POST', body: JSON.stringify(input) });
}

export async function updateAdminQuiz(quizId: string, quiz: {
    title: string;
    themeId: string;
    questionsPerGame: number;
    questions: Array<Omit<AdminQuestion, 'id'> & { id?: string }>;
}): Promise<AdminQuiz> {
    return adminRequest(`/api/v1/admin/quizes/${encodeURIComponent(quizId)}`, {
        method: 'PUT',
        body: JSON.stringify(quiz)
    });
}

export async function publishAdminQuiz(quizId: string): Promise<AdminQuiz> {
    return adminRequest(`/api/v1/admin/quizes/${encodeURIComponent(quizId)}/publish`, { method: 'POST' });
}

export async function archiveAdminQuiz(quizId: string): Promise<AdminQuiz> {
    return adminRequest(`/api/v1/admin/quizes/${encodeURIComponent(quizId)}/archive`, { method: 'POST' });
}

export async function deleteAdminQuiz(quizId: string): Promise<void> {
    await adminRequest(`/api/v1/admin/quizes/${encodeURIComponent(quizId)}`, { method: 'DELETE' });
}

export async function validateQuizImport(document: QuizImportDocument): Promise<ImportValidation> {
    return adminRequest('/api/v1/admin/quizes/import/validate', {
        method: 'POST',
        body: JSON.stringify(document)
    });
}

export async function importQuiz(document: QuizImportDocument): Promise<AdminQuiz> {
    return adminRequest('/api/v1/admin/quizes/import', {
        method: 'POST',
        body: JSON.stringify(document)
    });
}

export async function downloadQuizExport(quiz: QuizListItem): Promise<void> {
    const response = await adminFetch(`/api/v1/admin/quizes/${encodeURIComponent(quiz.id)}/export`);
    if (!response.ok) {
        throw await toApiError(response);
    }

    const url = URL.createObjectURL(await response.blob());
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${toFileName(quiz.title)}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
}

async function adminRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
    const response = await adminFetch(path, init);
    if (!response.ok) {
        throw await toApiError(response);
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return response.json() as Promise<T>;
}

async function adminFetch(path: string, init: RequestInit = {}): Promise<Response> {
    const accessToken = await getAccessToken();
    if (!accessToken) {
        throw new Error('Your session has expired. Sign in again to continue.');
    }

    return fetch(`${apiBaseUrl}${path}`, {
        ...init,
        headers: {
            Accept: 'application/json',
            Authorization: `Bearer ${accessToken}`,
            ...(init.body ? { 'Content-Type': 'application/json' } : {}),
            ...init.headers
        }
    });
}

async function toApiError(response: Response): Promise<Error> {
    const body = await response.json().catch(() => ({})) as { message?: string };
    return new Error(body.message ?? `Request failed with status ${response.status}.`);
}

function toFileName(value: string): string {
    return value.trim().replace(/[^a-z0-9]+/gi, '-').replace(/(^-|-$)/g, '') || 'quiz-export';
}
