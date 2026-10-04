import { getDocument, updateDocument, deleteDocument } from './api.js';

const title = document.getElementById('title');
const message = document.getElementById('message');
const details = document.getElementById('details');
const editForm = document.getElementById('edit-form');
const editError = document.getElementById('edit-error');
const deleteButton = document.getElementById('delete-button');

const id = new URLSearchParams(window.location.search).get('id');
let currentDocument = null;

function addRow(label, value) {
    const dt = document.createElement('dt');
    dt.textContent = label;
    const dd = document.createElement('dd');
    dd.textContent = value;
    details.append(dt, dd);
}

function renderDocument(doc) {
    currentDocument = doc;
    title.textContent = doc.fileName;

    details.replaceChildren();
    addRow('ID', doc.id);
    addRow('Description', doc.description || '(none)');
    addRow('Uploaded', new Date(doc.uploadedAt).toLocaleString());

    editForm.elements.fileName.value = doc.fileName;
    editForm.elements.description.value = doc.description ?? '';
}

async function loadDetail() {
    if (!id) {
        message.textContent = 'No document id given';
        return;
    }

    try {
        renderDocument(await getDocument(id));
        editForm.hidden = false;
        deleteButton.hidden = false;
    } catch (error) {
        message.textContent = error.status === 404
            ? 'Document not found'
            : `Could not load the document (${error.message})`;
    }
}

async function handleSave(event) {
    event.preventDefault();

    const fileName = editForm.elements.fileName.value.trim();
    const description = editForm.elements.description.value.trim();

    if (fileName === '') {
        editError.textContent = 'File name must not be empty';
        return;
    }
    editError.textContent = '';

    try {
        const updated = await updateDocument(currentDocument.id, {
            ...currentDocument,
            fileName,
            description,
        });
        renderDocument(updated);
    } catch (error) {
        editError.textContent = `Could not save the changes (${error.message})`;
    }
}

async function handleDelete() {
    if (!confirm(`Delete "${title.textContent}"?`)) {
        return;
    }
    try {
        await deleteDocument(id);
        window.location.href = 'index.html';
    } catch (error) {
        message.textContent = `Could not delete the document (${error.message})`;
    }
}

editForm.addEventListener('submit', handleSave);
deleteButton.addEventListener('click', handleDelete);
loadDetail();