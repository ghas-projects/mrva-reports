/**
 * Fetches a gzip-compressed file and decompresses it using the browser's
 * native DecompressionStream API (runs in C++, much faster than .NET's
 * GZipStream compiled to WASM).
 *
 * @param {string} url - Relative or absolute URL of the .gz file.
 * @returns {Promise<Uint8Array>} The decompressed bytes.
 */
export async function fetchAndDecompress(url) {

    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`Failed to fetch ${url}: ${response.status}`);
    }

    const downloadReader = response.body.getReader();
    const compressedChunks = [];

    while (true) {
        const { done, value } = await downloadReader.read();
        if (done) break;
        compressedChunks.push(value);
    }

    // ── Decompress phase ────────────────────────────────────────────
    // If the browser transparently decompressed the response (i.e. the
    // server applied Content-Encoding: gzip and the browser stripped it),
    // the body is already raw bytes. We detect this by checking whether
    // the first two bytes are the gzip magic number (0x1f 0x8b).
    const compressedBlob = new Blob(compressedChunks);
    const header = new Uint8Array(await compressedBlob.slice(0, 2).arrayBuffer());
    const isGzip = header[0] === 0x1f && header[1] === 0x8b;

    let resultStream;
    if (isGzip) {
        const ds = new DecompressionStream('gzip');
        resultStream = compressedBlob.stream().pipeThrough(ds);
    } else {
        // Already decompressed by the browser
        resultStream = compressedBlob.stream();
    }

    const reader = resultStream.getReader();
    const chunks = [];
    let totalLength = 0;

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        chunks.push(value);
        totalLength += value.length;
    }

    const result = new Uint8Array(totalLength);
    let offset = 0;
    for (const chunk of chunks) {
        result.set(chunk, offset);
        offset += chunk.length;
    }

    return result;
}
