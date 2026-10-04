const BASE_URL = '/api/documents';

async function request(url, options) {
    const response = await fetch(url, options);
    if (!response.ok) {
        const error = new Error(`error ${response.status}`);
        error.status = response.status;
        throw error;
    }
    return response;
}

export async function getDocuments() {
    const response = await request(BASE_URL);
    const result = await response.json();
    return result.data;
}

export async function getDocument(id) {
    const response = await request(`${BASE_URL}/${encodeURIComponent(id)}`);
    const result = await response.json();
    return result.data;
}

export async function createDocument({ fileName, description }) {
    const response = await request(BASE_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName, description }),
    });
    return response.json();
}

export async function deleteDocument(id) {
    await request(`${BASE_URL}/${encodeURIComponent(id)}`, { method: 'DELETE' });
}

export async function updateDocument(id, doc) {
    const response = await request(`${BASE_URL}/${encodeURIComponent(id)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(doc),
    });
    return response.json();
}