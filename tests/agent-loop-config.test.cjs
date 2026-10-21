'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { validateProjectConfig } = require('../scripts/validate-agent-loop-config.cjs');

function project(repo, overrides = {}) {
  return {
    repo,
    enabled: true,
    capabilities: {
      scan: true,
      issue_claim: false,
      issue_write: false,
      develop: false,
      review: false,
      merge: false,
      close_issue: false,
      deploy: false
    },
    limits: { max_parallel_jobs: 1, daily_dispatch_budget: 0 },
    runtime: { name: 'node', minimum_version: '22.13.0' },
    dependencies: [],
    test_commands: ['node --test'] ,
    ...overrides
  };
}

function validConfig(projects = [project('henryfir456/agent-development-loop')]) {
  return {
    schema_version: 1,
    controller: {
      state_path: '.adl-state/controller.sqlite',
      scheduler: { enabled: false, interval_seconds: 300 },
      dispatcher: { enabled: false },
      limits: {
        max_parallel_jobs: 2,
        max_parallel_per_repo: 1,
        daily_dispatch_budget: 0,
        max_issue_pages_per_repo: 20
      }
    },
    projects
  };
}

test('accepts an explicitly scan-only project configuration', () => {
  assert.deepEqual(validateProjectConfig(validConfig()), { valid: true, errors: [] });
});

test('accepts the checked-in example and all three onboarded repositories', () => {
  const example = require('../config/agent-loop-projects.example.json');
  assert.deepEqual(validateProjectConfig(example), { valid: true, errors: [] });
  assert.deepEqual(example.projects.map((entry) => entry.repo).sort(), [
    'henryfir456/DailyCheckIn_Android',
    'henryfir456/agent-development-loop',
    'henryfir456/bento-order-app'
  ].sort());
});

test('rejects unknown keys and misspelled capabilities', () => {
  const extraKey = validConfig();
  extraKey.controller.dispatcher.unattended = true;
  assert.equal(validateProjectConfig(extraKey).valid, false);

  const misspelledCapability = validConfig();
  misspelledCapability.projects[0].capabilities.deply = true;
  assert.equal(validateProjectConfig(misspelledCapability).valid, false);
});

test('rejects absolute, traversing, and NUL-containing state paths', () => {
  for (const statePath of ['../state.sqlite', 'nested/../state.sqlite', 'C:\\data\\state.sqlite', '\\foo', '\\\\server\\share\\state.sqlite', 'state\u0000.sqlite']) {
    const value = validConfig();
    value.controller.state_path = statePath;
    assert.equal(validateProjectConfig(value).valid, false, JSON.stringify(statePath));
  }
});

test('published JSON Schema rejects the same unsafe state paths as the runtime validator', () => {
  const schemaPath = path.join(__dirname, '..', 'schemas', 'agent-loop-projects.schema.json');
  const schema = JSON.parse(fs.readFileSync(schemaPath, 'utf8'));
  const statePathPattern = new RegExp(schema.properties.controller.properties.state_path.pattern);
  assert.equal(statePathPattern.test('.adl-state/controller.sqlite'), true);
  for (const statePath of ['/tmp/state.sqlite', '../state.sqlite', 'nested/../state.sqlite',
    'C:\\data\\state.sqlite', '\\foo', '\\\\server\\share\\state.sqlite', 'state\0.sqlite']) {
    assert.equal(statePathPattern.test(statePath), false, JSON.stringify(statePath));
  }
});

test('rejects duplicate or malformed repository identities', () => {
  assert.equal(validateProjectConfig(validConfig([
    project('henryfir456/agent-development-loop'),
    project('henryfir456/agent-development-loop')
  ])).valid, false);
  assert.equal(validateProjectConfig(validConfig([project('agent-development-loop')])).valid, false);
  assert.equal(validateProjectConfig(validConfig([{ ...project(undefined) }])).valid, false);
});

test('rejects negative, non-finite, missing, or unsafe budgets', () => {
  for (const budget of [-1, Number.POSITIVE_INFINITY, null, undefined]) {
    const value = validConfig();
    if (budget === undefined) delete value.controller.limits.daily_dispatch_budget;
    else value.controller.limits.daily_dispatch_budget = budget;
    assert.equal(validateProjectConfig(value).valid, false, `controller budget ${budget}`);
  }
  const repoBudget = validConfig();
  repoBudget.projects[0].limits.daily_dispatch_budget = -1;
  assert.equal(validateProjectConfig(repoBudget).valid, false);
});

test('does not infer write permissions from omitted or inconsistent capability flags', () => {
  const omitted = validConfig();
  delete omitted.projects[0].capabilities.issue_write;
  assert.equal(validateProjectConfig(omitted).valid, false);

  const unsafe = validConfig();
  unsafe.projects[0].capabilities.issue_write = true;
  assert.equal(validateProjectConfig(unsafe).valid, false);
});

test('rejects write capabilities unless their full prerequisite chain and budgets are enabled', () => {
  const unsafe = validConfig();
  unsafe.controller.dispatcher.enabled = true;
  unsafe.controller.limits.daily_dispatch_budget = 1;
  Object.assign(unsafe.projects[0].capabilities, {
    issue_claim: true,
    issue_write: true,
    develop: true,
    review: true,
    merge: true,
    close_issue: true,
    deploy: true
  });
  unsafe.projects[0].limits.daily_dispatch_budget = 1;
  assert.equal(validateProjectConfig(unsafe).valid, true);

  unsafe.projects[0].capabilities.merge = false;
  assert.equal(validateProjectConfig(unsafe).valid, false);
});
