const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const projectRoot = path.resolve(__dirname, '..', '..');
const runtimeRoot = path.join(projectRoot, 'Assets', 'Project');
// This is the complete unsupported set found by auditing runtime UI text
// against Assets/Project/Material/Font/zh-cn.ttf on 2026-09-09.
const unsupportedGlyphs = /[•‹›ℹ↻⌫▶◀♦♥♡⚔⚙⚡⚠✓✕➔✦🛡🌐🏃]/u;

function collectRuntimeTextFiles(directory, results = []) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (entry.name !== 'Tests') collectRuntimeTextFiles(fullPath, results);
      continue;
    }

    if (entry.name.endsWith('.cs') || entry.name.endsWith('.uxml') ||
        entry.name.endsWith('.json')) {
      results.push(fullPath);
    }
  }
  return results;
}

test('runtime UI avoids glyphs missing from the bundled WebGL font', () => {
  const offenders = collectRuntimeTextFiles(runtimeRoot)
    .filter((file) => unsupportedGlyphs.test(fs.readFileSync(file, 'utf8')))
    .map((file) => path.relative(projectRoot, file).replaceAll('\\', '/'));

  assert.deepEqual(offenders, []);
});
