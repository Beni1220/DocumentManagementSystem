import { getDocuments, deleteDocument, uploadDocument, searchDocuments } from './api.js';

const list = document.getElementById('list');
const listError = document.getElementById('list-error');
const formError = document.getElementById('form-error');
const fileInput = document.getElementById('upload-document');
const uploadButton = document.getElementById('upload-button');
const searchForm = document.getElementById('search-form');
const searchInput = document.getElementById('search-query');
const searchStatus = document.getElementById('search-status');
const clearSearchButton = document.getElementById('clear-search');

async function loadDocuments() {
    try {
        const documents = await getDocuments();
        renderDocuments(documents);
        listError.textContent = '';
    } catch (error) {
        listError.textContent = `Could not load documents (${error.message})`;
    }
}

function renderDocuments(documents) {
    list.replaceChildren();

    if (documents.length === 0) {
        const empty = document.createElement('li');
        empty.textContent = 'No documents yet';
        list.appendChild(empty);
        return;
    }

    for (const doc of documents) {
        list.appendChild(createListItem(doc));
    }
}

function createListItem(doc) {
    const item = document.createElement('li');

    const link = document.createElement('a');
    link.textContent = doc.fileName;
    link.href = `detail.html?id=${doc.id}`;

    const description = document.createElement('span');
    description.textContent = doc.description ? ` - ${doc.description}` : '';

    const deleteButton = document.createElement('button');
    deleteButton.textContent = 'Delete';
    deleteButton.className = 'danger';
    deleteButton.addEventListener('click', () => handleDelete(doc));

    item.append(link, description, deleteButton);
    return item;
}

async function handleDelete(doc) {
    if (!confirm(`Delete "${doc.fileName}"?`)) {
        return;
    }
    try {
        await deleteDocument(doc.id);
        if (searchInput.value.trim().length >= 3) {
            await searchForDocuments();
        } else {
            await loadDocuments();
        }
    } catch (error) {
        listError.textContent = `Could not delete the document (${error.message})`;
    }
}

fileInput.addEventListener('change', () => {
    uploadButton.disabled = fileInput.files.length === 0;
});

async function handleFileUpload() {
    if (uploadButton.disabled || fileInput.files.length === 0) {
        return;
    }
    formError.textContent = '';

    try {
        uploadButton.disabled = true;
        await uploadDocument(fileInput.files[0]);
        searchInput.value = '';
        searchStatus.textContent = '';
        await loadDocuments();

        fileInput.value = '';

    } catch (error) {
        formError.textContent = `Could not upload the document (${error.message})`;
    } finally {
        uploadButton.disabled = fileInput.files.length === 0;
    }

}

async function searchForDocuments() {
    const query = searchInput.value.trim();
    if (query.length < 3) {
        searchStatus.textContent = 'Enter at least 3 characters.';
        return;
    }

    try {
        searchStatus.textContent = 'Searching…';
        const results = await searchDocuments(query);
        const documents = [];
        for (const result of results) {
            documents.push({
                id: result.documentId,
                fileName: result.fileName,
                description: result.description,
            });
        }
        renderDocuments(documents);
        if (results.length === 0) {
            searchStatus.textContent = 'No matching documents found.';
        } else {
            searchStatus.textContent = `Matching documents: ${results.length}`;
        }
        listError.textContent = '';
    } catch (error) {
        searchStatus.textContent = '';
        listError.textContent = `Search failed (${error.message}).`;
    }
}

searchForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    await searchForDocuments();
});

clearSearchButton.addEventListener('click', async () => {
    searchInput.value = '';
    searchStatus.textContent = '';
    await loadDocuments();
});

uploadButton.addEventListener('click', handleFileUpload);

loadDocuments();
