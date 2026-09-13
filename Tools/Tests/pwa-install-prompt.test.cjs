const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

const source = fs.readFileSync(path.join(
  __dirname,
  '../../Assets/WebGLTemplates/MathWorldPWA/TemplateData/pwa-install.js'), 'utf8');

function setup({ android = true, installed = false } = {}) {
  const listeners = {};
  const elementListeners = {};
  const elements = {
    'pwa-install-prompt': { hidden: true },
    'pwa-install-button': { addEventListener: (name, callback) => { elementListeners[name] = callback; } },
    'pwa-install-dismiss': { addEventListener: (name, callback) => { elementListeners[`dismiss-${name}`] = callback; } }
  };
  const window = {
    navigator: { userAgent: android ? 'Android Chrome' : 'Desktop Chrome', standalone: false },
    matchMedia: () => ({ matches: installed }),
    addEventListener: (name, callback) => { listeners[name] = callback; }
  };
  const document = { getElementById: id => elements[id] || null };
  vm.runInNewContext(source, { window, document, Promise });
  listeners.DOMContentLoaded();
  return { listeners, elementListeners, prompt: elements['pwa-install-prompt'] };
}

test('shows the install card on eligible Android browser visits', () => {
  const context = setup();
  context.listeners.beforeinstallprompt({ preventDefault() {} });
  assert.equal(context.prompt.hidden, false);
});

test('does not show on desktop or when already installed', () => {
  const desktop = setup({ android: false });
  desktop.listeners.beforeinstallprompt({ preventDefault() {} });
  assert.equal(desktop.prompt.hidden, true);

  const installed = setup({ installed: true });
  installed.listeners.beforeinstallprompt({ preventDefault() {} });
  assert.equal(installed.prompt.hidden, true);
});

test('native prompt runs only after the player taps Install', async () => {
  const context = setup();
  let promptCalls = 0;
  context.listeners.beforeinstallprompt({
    preventDefault() {},
    prompt: async () => { promptCalls += 1; },
    userChoice: Promise.resolve({ outcome: 'accepted' })
  });
  assert.equal(promptCalls, 0);
  await context.elementListeners.click();
  assert.equal(promptCalls, 1);
  assert.equal(context.prompt.hidden, true);
});

test('appinstalled immediately removes the card', () => {
  const context = setup();
  context.listeners.beforeinstallprompt({ preventDefault() {} });
  context.listeners.appinstalled();
  assert.equal(context.prompt.hidden, true);
});
