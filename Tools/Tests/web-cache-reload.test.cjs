const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const bridge = fs.readFileSync(path.join(__dirname, '../../Assets/Plugins/WebGL/PowerMathWebBridge.jslib'), 'utf8');

function setup({ storageBlocked = false, deleteFails = false } = {}) {
  const data = new Map();
  const deleted = [], reloaded = [];
  const library = {};
  const caches = {
    keys: async () => ['other-app', 'powermath-build-old'],
    delete: async name => { deleted.push(name); if (deleteFails) throw new Error('blocked'); }
  };
  const window = {
    caches,
    location: { pathname: '/game/', href: 'https://example.test/game/?keep=1',
      replace: value => reloaded.push(value) }
  };
  vm.runInNewContext(bridge, {
    LibraryManager: { library }, mergeInto: Object.assign, UTF8ToString: value => value,
    window, caches, URL, Promise, setTimeout: () => {},
    sessionStorage: {
      getItem: key => { if (storageBlocked) throw new Error('blocked'); return data.get(key); },
      setItem: (key, value) => data.set(key, value)
    }
  });
  return { library, deleted, reloaded };
}
const flush = () => new Promise(resolve => setImmediate(resolve));
test('cleans only owned cache and reloads each target at most once', async () => {
  const context = setup();
  context.library.PowerMathPurgeCacheAndReload('3.0');
  context.library.PowerMathPurgeCacheAndReload('3.0');
  await flush();
  assert.deepEqual(context.deleted, ['powermath-build-old']);
  assert.equal(context.reloaded.length, 1);
  assert.equal(new URL(context.reloaded[0]).searchParams.get('keep'), '1');
  assert.equal(new URL(context.reloaded[0]).searchParams.get('powermathBuild'), '3.0');
});
test('does not reload automatically without session storage loop guard', async () => {
  const context = setup({ storageBlocked: true });
  context.library.PowerMathPurgeCacheAndReload('3.0');
  await flush();
  assert.equal(context.reloaded.length, 0);
  assert.equal(context.deleted.length, 0);
});
test('cleanup rejection cannot create a reload loop', async () => {
  const context = setup({ deleteFails: true });
  context.library.PowerMathPurgeCacheAndReload('3.0');
  await flush();
  context.library.PowerMathPurgeCacheAndReload('3.0');
  await flush();
  assert.equal(context.reloaded.length, 1);
});
