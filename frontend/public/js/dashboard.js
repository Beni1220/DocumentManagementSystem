import { getDocuments, createDocument, deleteDocument } from './api.js';

const list = document.getElementById('list');
const listError = document.getElementById('list-error');
const form = document.getElementById('create-form');
const formError = document.getElementById('form-error');

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
        await loadDocuments();
    } catch (error) {
        listError.textContent = `Could not delete the document (${error.message})`;
    }
}

async function handleSubmit(event) {
    event.preventDefault();

    const fileName = form.elements.fileName.value.trim();
    const description = form.elements.description.value.trim();

    if (fileName === '') {
        formError.textContent = 'File name must not be empty';
        return;
    }
    formError.textContent = '';

    try {
        await createDocument({ fileName, description });
        form.reset();
        await loadDocuments();
    } catch (error) {
        formError.textContent = `Could not save the document (${error.message})`;
    }
}

form.addEventListener('submit', handleSubmit);
loadDocuments();