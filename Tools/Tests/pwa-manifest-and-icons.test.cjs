const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const projectRoot = path.join(__dirname, '../..');

test('Cloudflare _headers includes manifest MIME type and root icon headers', () => {
  const headersPath = path.join(projectRoot, 'Cloudflare/public/_headers');
  const content = fs.readFileSync(headersPath, 'utf8');

  assert.match(
    content,
    /\/manifest\.webmanifest[\s\S]*?Content-Type:\s*application\/manifest\+json/,
    'manifest.webmanifest must have application/manifest+json Content-Type'
  );
  assert.match(
    content,
    /\/favicon\.ico[\s\S]*?Content-Type:\s*image\/x-icon/,
    'favicon.ico must have image/x-icon Content-Type'
  );
  assert.match(
    content,
    /\/apple-touch-icon\*\.png[\s\S]*?Content-Type:\s*image\/png/,
    'apple-touch-icon must have image/png Content-Type'
  );
});

test('index.html includes complete favicon, apple-touch-icon, and OpenGraph/Twitter tags', () => {
  const indexPath = path.join(projectRoot, 'Assets/WebGLTemplates/MathWorldPWA/index.html');
  const content = fs.readFileSync(indexPath, 'utf8');

  // Favicon links
  assert.match(content, /<link\s+rel="shortcut icon"\s+href="TemplateData\/favicon\.ico"/);
  assert.match(content, /<link\s+rel="icon"\s+type="image\/x-icon"\s+href="TemplateData\/favicon\.ico"/);
  assert.match(content, /<link\s+rel="icon"\s+type="image\/png"\s+sizes="192x192"/);
  assert.match(content, /<link\s+rel="icon"\s+type="image\/png"\s+sizes="512x512"/);

  // Apple touch icon (generic fallback and precomposed for older iOS)
  assert.match(content, /<link\s+rel="apple-touch-icon"\s+href="TemplateData\/icons\/mathworld-180\.png"/);
  assert.match(content, /<link\s+rel="apple-touch-icon"\s+sizes="180x180"\s+href="TemplateData\/icons\/mathworld-180\.png"/);
  assert.match(content, /<link\s+rel="apple-touch-icon-precomposed"\s+href="TemplateData\/icons\/mathworld-180\.png"/);

  // Open Graph meta tags for social share sheet preview
  assert.match(content, /<meta\s+property="og:type"\s+content="website"/);
  assert.match(content, /<meta\s+property="og:title"/);
  assert.match(content, /<meta\s+property="og:image"\s+content="TemplateData\/icons\/mathworld-512\.png"/);
  assert.match(content, /<meta\s+property="og:image:width"\s+content="512"/);
  assert.match(content, /<meta\s+property="og:image:height"\s+content="512"/);

  // Twitter card
  assert.match(content, /<meta\s+name="twitter:card"\s+content="summary"/);
  assert.match(content, /<meta\s+name="twitter:image"\s+content="TemplateData\/icons\/mathworld-512\.png"/);
});

test('mathworld-180.png is opaque RGB with no alpha transparency', () => {
  const iconPath = path.join(
    projectRoot,
    'Assets/WebGLTemplates/MathWorldPWA/TemplateData/icons/mathworld-180.png'
  );
  const buf = fs.readFileSync(iconPath);
  const width = buf.readUInt32BE(16);
  const height = buf.readUInt32BE(20);
  const colorType = buf.readUInt8(25);

  assert.equal(width, 180, 'Width must be 180');
  assert.equal(height, 180, 'Height must be 180');
  assert.equal(colorType, 2, 'Color type must be 2 (RGB without alpha channel for iOS compatibility)');
});

test('Prepare-CloudflarePagesPackage.ps1 copies root-level fallback icons', () => {
  const scriptPath = path.join(projectRoot, 'Tools/Prepare-CloudflarePagesPackage.ps1');
  const content = fs.readFileSync(scriptPath, 'utf8');

  assert.match(content, /favicon\.ico/, 'Script must copy favicon.ico to root');
  assert.match(content, /apple-touch-icon\.png/, 'Script must copy apple-touch-icon.png to root');
  assert.match(content, /apple-touch-icon-precomposed\.png/, 'Script must copy apple-touch-icon-precomposed.png to root');
});
