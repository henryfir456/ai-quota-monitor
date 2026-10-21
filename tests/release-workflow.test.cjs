'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const workflow = fs.readFileSync(path.join(__dirname, '../.github/workflows/release.yml'), 'utf8');

test('v0.1.2 release is serialized across refs and only dispatches from main', () => {
  assert.match(workflow, /workflow_dispatch:/);
  assert.doesNotMatch(workflow, /^\s+push:/m);
  assert.match(workflow, /group: windows-release-v0\.1\.2/);
  assert.match(workflow, /GITHUB_REF_NAME -ne 'main'/);
  assert.match(workflow, /DISPATCH_RELEASE_TAG -ne \$expectedTag/);
});

test('publisher atomically creates and verifies the exact tested source tag after Windows gates', () => {
  const publisherStart = workflow.indexOf('  publish-prerelease:');
  const publisher = workflow.slice(publisherStart);
  assert.ok(publisherStart >= 0);
  assert.match(publisher, /needs: smoke-and-verify/);
  assert.match(publisher, /gh api --method POST "repos\/\$GITHUB_REPOSITORY\/git\/refs"/);
  assert.ok(publisher.includes('gh api "repos/$GITHUB_REPOSITORY/git/ref/tags/$RELEASE_TAG" >/dev/null 2>&1'));
  assert.match(publisher, /-f "sha=\$EXPECTED_SHA"/);
  assert.match(publisher, /actual_tag_sha=.*git\/ref\/tags\/\$RELEASE_TAG/);
  assert.match(publisher, /actual_tag_sha" != "\$EXPECTED_SHA/);
  assert.match(publisher, /gh release create "\$RELEASE_TAG"[\s\S]*--verify-tag --prerelease/);
  assert.doesNotMatch(publisher, /softprops\/action-gh-release/);
});
