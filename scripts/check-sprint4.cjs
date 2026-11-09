// Manueller Browser-Test. Playwright muss installiert sein (siehe README).
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const path = require('node:path');

async function main() {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage();
        await page.goto('http://localhost/');
        await page.locator('#upload-document').setInputFiles(
            path.join(__dirname, '..', 'semester-project.pdf'));

        const uploadResponse = page.waitForResponse(response =>
            response.url().includes('/uploadDocument') && response.request().method() === 'POST');
        await page.locator('#upload-button').click();
        const response = await uploadResponse;
        assert.equal(response.status(), 201, 'PDF-Upload muss erfolgreich sein');
        const document = await response.json();
        console.log(`PDF hochgeladen: ID ${document.id}`);

        // Bei mehreren Seiten braucht die OCR etwas Zeit.
        const deadline = Date.now() + 240000;
        let indexed = false;
        while (Date.now() < deadline) {
            const result = await page.request.get(`http://localhost:9200/documents/_doc/${document.id}`);
            if (result.ok()) {
                const entry = await result.json();
                assert.match(entry._source.textContent, /Tesseract/i);
                indexed = true;
                break;
            }
            await page.waitForTimeout(2000);
        }
        assert.ok(indexed, 'OCR-Text muss innerhalb von 4 Minuten im Index stehen');

        await page.locator('#search-query').fill('Tesseract');
        const searchResponse = page.waitForResponse(response => response.url().includes('/search?q='));
        await page.locator('#search-form').evaluate(form => form.requestSubmit());
        assert.equal((await searchResponse).status(), 200);
        await page.locator(`#list a[href="detail.html?id=${document.id}"]`).waitFor();
        await page.screenshot({ path: path.join(__dirname, '..', 'sprint4-search.png'), fullPage: true });
        console.log(`OK: OCR-Inhalt und Suchtreffer fuer Dokument ${document.id} bestaetigt.`);
        console.log('Screenshot: sprint4-search.png.');
        if (!process.argv.includes('--check-delete')) {
            console.log('Das Testdokument bleibt als Beispiel gespeichert.');
        }

        const kibanaStatus = await page.request.get('http://localhost:5601/api/status');
        assert.equal(kibanaStatus.status(), 200, 'Kibana muss erreichbar sein');
        assert.equal((await kibanaStatus.json()).status.overall.level, 'available');

        // Eine neue Anfrage muss durch Logstash bis nach Elasticsearch kommen.
        const marker = `sprint4check${Date.now()}`;
        await page.goto(`http://localhost/?check=${marker}`);
        let logFound = false;
        const logDeadline = Date.now() + 60000;
        while (Date.now() < logDeadline) {
            const logs = await page.request.post('http://localhost:9200/dms-logs-*/_search', {
                data: { query: { match_phrase: { message: marker } } }
            });
            if (logs.ok() && (await logs.json()).hits.hits.length > 0) {
                logFound = true;
                break;
            }
            await page.waitForTimeout(2000);
        }
        assert.ok(logFound, 'Neuer Webseitenzugriff muss im Log-Index erscheinen');
        console.log('OK: Kibana verfuegbar und neuer Zugriff ueber Logstash indexiert.');

        if (process.argv.includes('--check-delete')) {
            // Nur den in diesem Durchlauf angelegten Test-Upload löschen.
            await page.locator('#search-query').fill('Tesseract');
            const search = page.waitForResponse(response => response.url().includes('/search?q='));
            await page.locator('#search-form').evaluate(form => form.requestSubmit());
            await search;
            const row = page.locator('#list li').filter({
                has: page.locator(`a[href="detail.html?id=${document.id}"]`)
            });
            await row.waitFor();
            page.once('dialog', dialog => dialog.accept());
            const deletion = page.waitForResponse(response =>
                response.request().method() === 'DELETE' && response.url().endsWith(`/documents/${document.id}`));
            await row.getByRole('button', { name: 'Delete', exact: true }).click();
            assert.equal((await deletion).status(), 204);
            await page.waitForFunction(id =>
                !document.querySelector(`#list a[href="detail.html?id=${id}"]`), document.id);
            assert.equal(await page.locator('#search-query').inputValue(), 'Tesseract');

            const detail = await page.request.get(`http://localhost/api/documents/${document.id}`);
            assert.equal(detail.status(), 404);
            const indexEntry = await page.request.get(`http://localhost:9200/documents/_doc/${document.id}`);
            assert.equal(indexEntry.status(), 404);

            // Simuliert einen Worker, der erst nach dem Löschen fertig wird.
            const lateIndex = await page.request.put(`http://localhost:9200/documents/_doc/${document.id}?refresh=wait_for`, {
                data: { documentId: document.id, fileName: document.fileName, textContent: 'Tesseract' }
            });
            assert.ok(lateIndex.ok());
            try {
                const afterDelete = await page.request.get('http://localhost/api/documents/search?q=Tesseract');
                assert.equal(afterDelete.status(), 200);
                const results = (await afterDelete.json()).data;
                assert.ok(!results.some(result => result.documentId === document.id));
            } finally {
                await page.request.delete(`http://localhost:9200/documents/_doc/${document.id}?refresh=wait_for`);
            }
            console.log('OK: Geloeschtes Dokument fehlt auch nach verspaeteter Indexierung in der Suche.');
        }
    } finally {
        await browser.close();
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
