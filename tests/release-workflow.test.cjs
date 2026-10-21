'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const workflow = fs.readFileSync(path.join(__dirname, '../.github/workflows/release.yml'), 'utf8');

test('v0.1.2 release only starts from a fixed manual dispatch on main', () => {
  assert.match(workflow, /workflow_dispatch:/);
  assert.doesNotMatch(workflow, /^\s+push:/m);
  assert.match(workflow, /group: windows-release-v0\.1\.2/);
  assert.match(workflow, /GITHUB_EVENT_NAME -ne 'workflow_dispatch'.*GITHUB_REF_NAME -ne 'main'/);
  assert.match(workflow, /DISPATCH_RELEASE_TAG -ne \$expectedTag/);
});

test('publisher atomically creates and verifies the exact tested source tag after Windows gates', () => {
  const publisherStart = workflow.indexOf('  publish-prerelease:');
  const publisher = workflow.slice(publisherStart);
  assert.ok(publisherStart >= 0);
  assert.match(publisher, /needs: smoke-and-verify/);
  assert.match(publisher, /gh api --method POST "repos\/\$GITHUB_REPOSITORY\/git\/refs"/);
  assert.match(publisher, /Existing v0\.1\.2 tag matches the exact tested source SHA; continuing an interrupted release/);
  assert.match(publisher, /existing_tag_sha.*!=.*EXPECTED_SHA/);
  assert.match(publisher, /release_status.*404/);
  assert.match(publisher, /A v0\.1\.2 release already exists; refusing to replace it/);
  assert.match(publisher, /-f "sha=\$EXPECTED_SHA"/);
  assert.match(publisher, /actual_tag_sha=.*resolve_tag_commit/);
  assert.match(publisher, /actual_tag_sha" != "\$EXPECTED_SHA/);
  assert.match(publisher, /gh release create "\$RELEASE_TAG"[\s\S]*--verify-tag --prerelease/);
  assert.doesNotMatch(publisher, /softprops\/action-gh-release/);
});
