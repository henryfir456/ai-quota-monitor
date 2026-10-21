'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const {spawnSync} = require('node:child_process');
const test = require('node:test');

const root = path.resolve(__dirname, '..');
const read = (file) => fs.readFileSync(path.join(root, file), 'utf8');

test('shared policy defines every blocker class and bounded recovery', () => {
  const policy = read('docs/policies/shared-agent-policy.md');
  const categories = [
    'CODE_FAILURE', 'TEST_FAILURE', 'CI_QUOTA_EXHAUSTED', 'ENVIRONMENT_BLOCKED',
    'POLICY_BLOCKED', 'EXTERNAL_APPROVAL_REQUIRED', 'HUMAN_TEST_PENDING', 'UNKNOWN'
  ];
  for (const category of categories) assert.match(policy, new RegExp('`' + category + '`'));
  assert.match(policy, /最多 3 次/);
  assert.match(policy, /UNKNOWN_EXHAUSTED/);
  assert.match(policy, /本機／備援驗證不會變成 GitHub Required Check 通過/);
  assert.match(policy, /正式部署與正式資料 migration 分開判定/);
});

test('draft PR readiness requires complete same-head evidence and a separate re-read before merge', () => {
  const policy = read('docs/policies/shared-agent-policy.md');
  for (const evidence of ['acceptance', 'privacyScan', 'retrospective']) assert.ok(policy.includes(evidence));
  assert.match(policy, /`MARK_PR_READY_FOR_REVIEW`/);
  assert.match(policy, /該輪不得緊接著合併/);
  assert.match(policy, /決策輸出不代表 PR 已升級/);
});

test('root and tool adapters converge on one policy source without copied policy text', () => {
  const rootInstructions = read('AGENTS.md');
  const claude = read('CLAUDE.md');
  const copilot = read('.github/copilot-instructions.md');
  const adapters = read('docs/agent-adapters/README.md');
  assert.match(rootInstructions, /docs\/policies\/shared-agent-policy\.md/);
  assert.match(claude, /@AGENTS\.md/);
  assert.match(copilot, /docs\/policies\/shared-agent-policy\.md/);
  for (const surface of ['Codex CLI', 'Claude Code', 'GitHub Copilot']) assert.ok(adapters.includes(surface));
  assert.match(adapters, /runtime/);
  assert.match(adapters, /GitHub code review/);
});

test('moved policy document has resolvable local Markdown links and references', () => {
  const file = path.join(root, 'docs/policies/agent-workflow.md');
  const content = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(content, /\.\.\/docs\//);
  for (const [, target] of content.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const href = target.split('#', 1)[0];
    if (!href || /^[a-z][a-z0-9+.-]*:/i.test(href) || href.startsWith('//')) continue;
    assert.ok(fs.existsSync(path.resolve(path.dirname(file), decodeURIComponent(href))), `missing policy link: ${target}`);
  }
});

test('policy sync checks by default and replaces only an explicitly identified ADL mirror', (context) => {
  const target = fs.mkdtempSync(path.join(os.tmpdir(), 'adl-policy-target-'));
  context.after(() => fs.rmSync(target, {recursive: true, force: true}));
  const script = path.join(root, 'scripts/sync-shared-agent-policy.cjs');
  fs.writeFileSync(path.join(target, 'package.json'), JSON.stringify({name: 'agent-development-loop'}));
  const policyPath = path.join(target, 'docs/policies/shared-agent-policy.md');
  const adaptersPath = path.join(target, 'docs/agent-adapters/README.md');
  fs.mkdirSync(path.dirname(policyPath), {recursive: true});
  fs.mkdirSync(path.dirname(adaptersPath), {recursive: true});
  fs.writeFileSync(policyPath, 'stale policy\n');
  fs.writeFileSync(adaptersPath, 'stale adapter\n');

  const check = spawnSync(process.execPath, [script, target], {encoding: 'utf8'});
  assert.equal(check.status, 1);
  assert.match(check.stdout, /DIFFERENT/);
  const guardedWrite = spawnSync(process.execPath, [script, target, '--write'], {encoding: 'utf8'});
  assert.equal(guardedWrite.status, 2);
  assert.equal(fs.readFileSync(policyPath, 'utf8'), 'stale policy\n');

  const sync = spawnSync(process.execPath, [script, target, '--write', '--replace-existing'], {encoding: 'utf8'});
  assert.equal(sync.status, 0, sync.stderr);
  assert.equal(fs.readFileSync(policyPath, 'utf8'), read('docs/policies/shared-agent-policy.md'));
  assert.equal(fs.readFileSync(adaptersPath, 'utf8'), read('docs/agent-adapters/README.md'));
  const verified = spawnSync(process.execPath, [script, target], {encoding: 'utf8'});
  assert.equal(verified.status, 0, verified.stderr);
  assert.match(verified.stdout, /PASS/);
  fs.writeFileSync(path.join(target, 'package.json'), JSON.stringify({name: 'wrong-repository'}));
  const wrongTarget = spawnSync(process.execPath, [script, target, '--write', '--replace-existing'], {encoding: 'utf8'});
  assert.equal(wrongTarget.status, 2);
  assert.match(wrongTarget.stderr, /identity does not match/);
});

test('policy sync refuses symlinked target directories', (context) => {
  const base = fs.mkdtempSync(path.join(os.tmpdir(), 'adl-policy-symlink-'));
  context.after(() => fs.rmSync(base, {recursive: true, force: true}));
  const target = path.join(base, 'target');
  const outside = path.join(base, 'outside');
  fs.mkdirSync(target);
  fs.mkdirSync(outside);
  fs.writeFileSync(path.join(target, 'package.json'), JSON.stringify({name: 'agent-development-loop'}));
  fs.symlinkSync(outside, path.join(target, 'docs'), 'dir');
  const script = path.join(root, 'scripts/sync-shared-agent-policy.cjs');
  const result = spawnSync(process.execPath, [script, target, '--write'], {encoding: 'utf8'});
  assert.equal(result.status, 2);
  assert.match(result.stderr, /symbolic links/);
  assert.deepEqual(fs.readdirSync(outside), []);
});
